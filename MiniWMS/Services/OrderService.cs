/* ========================================================================
* MIT License
*
* Copyright (c) 2021 Weland Solutions AB
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*
* ======================================================================*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Weland.CompactTalk;
using Weland.CompactTalk.Client;
using Weland.CompactTalk.Framework.OrderManagement;
using Weland.Ct.Api.Sample.MiniWMS.Models;

namespace Weland.Ct.Api.Sample.MiniWMS.Services
{
    public delegate void OrderStatusChanged(CTOrderStatusChangeEvent evt);
    public delegate void QueueChanged(CTQueueChangeEvent evt);

    /// <summary>
    /// This class keeps orders and handles the Compact Talk communication
    /// </summary>
    public class OrderService
    {
        // Event when order status changes in Compact Talk
        public event OrderStatusChanged OnOrderStatusChanged;
        // Event when order queue changes in Compact Talk
        public event QueueChanged OnQueueChanged;
        // The Compact Talk WCF connection
        private CTConnection _compactTalkConnection;
        //Index of orders sent to Compact Talk
        private readonly Dictionary<int, OrderRecord> _ordersByCTId = new Dictionary<int, OrderRecord>();

        private readonly ArticleService _articleService;

        public OrderService(ArticleService articleService)
        {
            _articleService = articleService;
        }

        public void Initialize(ISynchronizeInvoke synchronizeInvoke)
        {
            CtConnect(synchronizeInvoke);
            GetStoredOrders();
            SynchronizeOrders();
        }

        public void ShutDown()
        {
            _compactTalkConnection.Disconnect();
        }

        public OrderRecord CreateOrder(OrderRecord order)
        {
            // Create order in Compact Talk
            var orderId = _compactTalkConnection.Command.AddToQueue(
                transId: "MiniWMS:" + order.Id,
                elevatorId: order.Elevator,
                tray: order.TrayNo,
                trayCoord: string.Empty,
                opening: order.ServiceOpening,
                artNo: order.ArticleNo,
                artDescr: order.ArticleDesc,
                mode: (OrderMode)Enum.Parse(typeof(OrderMode), order.Operation),
                noReturnOfTray: 0,
                priority: 1,
                job: string.Empty,
                quantity: order.Quantity,
                currentBoxName: string.Empty,
                info1: string.Empty,
                info2: string.Empty,
                info3: string.Empty,
                info4: string.Empty,
                info5: string.Empty,
                activate: true
            );

            order.CTOrderId = orderId;
            order.Id = orderId;
            order.CTStatus = "Selected";

            //Add the order to the order index
            _ordersByCTId.Add(order.Id, order);

            //Save changes to the order list in the storage file
            SaveOrders();

            return order;
        }

        public void AcknowledgeOrder(int orderId, float quantity)
        {
            OrderRecord selectedOrder = _ordersByCTId[orderId];

            // Acknowledge order in Compact Talk
            _compactTalkConnection.Command.ExtAckOrder(
                elevatorId: selectedOrder.Elevator,
                opening: selectedOrder.ServiceOpening,
                quantity: quantity,
                usePanelQuantity: false
                );
        }

        public IReadOnlyCollection<OrderRecord> GetOrders()
        {
            return _ordersByCTId.Select(orders => orders.Value).ToList();
        }

        public bool TryGetOrder(int orderId, out OrderRecord record)
        {
            return _ordersByCTId.TryGetValue(orderId, out record);
        }

        private List<OrderRecord> GetStoredOrders()
        {
            //Deserialize order list from file storage
            using (Stream file = File.Open("MyOrders.dat", FileMode.OpenOrCreate))
            {
                if (file.Length > 0)
                {
                    var orders = JsonSerializer.DeserializeAsync<List<OrderRecord>>(file).Result;
                    orders.ForEach(order => _ordersByCTId.Add(order.CTOrderId, order));
                    return orders;
                }
            }

            return new List<OrderRecord>();
        }

        private void SaveOrders()
        {
            //Serialize order list to file storage
            using Stream orderFile = File.Open("MyOrders.dat", FileMode.Create);
            JsonSerializer.SerializeAsync(orderFile, GetOrders()).Wait();
        }

        private void SynchronizeOrders()
        {
            var inaktiveOrders = new List<OrderRecord>();

            //For each order in our order list, check to se if it matches Compact Talks order
            foreach (var order in GetOrders())
            {
                PickOrder ctOrder = _compactTalkConnection.Command.GetOrder(order.CTOrderId);
                if (ctOrder == null)
                {
                    //Compakt Talk has never seen an order with this id. This should never happen.
                    inaktiveOrders.Add(order);
                    continue;
                }
                //The order has been acknowledged while we where shut down and now only exist in historical storage
                else if (ctOrder.Status == OrderStatus.Historical)
                {
                    _articleService.UpdateArticle(order.ArticleId, ctOrder);

                    inaktiveOrders.Add(order);
                }
                else
                {
                    order.CTStatus = ctOrder.Status.ToString();
                }
            }

            inaktiveOrders.ForEach(order => _ordersByCTId.Remove(order.CTOrderId));

            //Save changes to the order list in the storage file
            SaveOrders();
        }

        private void OnOrderStatusChangedEvent(CTOrderStatusChangeEvent evt)
        {
            //Check to see if it's one of our orders we received the event for, if not just ignore the event
            if (!_ordersByCTId.ContainsKey(evt.Order.Id))
                return;

            OrderRecord order = _ordersByCTId[evt.Order.Id];

            //Search for the order in the order view
            order.CTStatus = evt.Order.Status.ToString();

            //If the new status is TaskDone
            if (evt.Order.Status == OrderStatus.TaskDone)
            {
                _articleService.UpdateArticle(order.ArticleId, evt.Order);
            }

            //Save changes to the order list in the storage file
            SaveOrders();

            OnOrderStatusChanged?.Invoke(evt);
        }

        private void OnQueueChangedEvent(CTQueueChangeEvent evt)
        {
            //Check to see if it's one of our orders we received the event for, if not just ignore the event
            if (!_ordersByCTId.ContainsKey(evt.Order.Id))
                return;

            OrderRecord order = _ordersByCTId[evt.Order.Id];

            //If the queue chane type is OrderDeleted
            if (evt.ChangeType == OrderQueueChangeType.OrderDeleted)
            {
                _ordersByCTId.Remove(order.CTOrderId);

                //Save changes to the order list in the storage file
                SaveOrders();

                OnQueueChanged?.Invoke(evt);
            }
        }
        private void CtConnect(ISynchronizeInvoke synchronizeInvoke)
        {
            //Connect to Compact Talk
            _compactTalkConnection = new CTConnection(synchronizeInvoke);
            _compactTalkConnection.OnOrderStatusChanged += new ClientOrderStatusChanged(OnOrderStatusChangedEvent);
            _compactTalkConnection.OnQueueChanged += new ClientQueueChanged(OnQueueChangedEvent);

            _compactTalkConnection.Connect("127.0.0.1");
        }
    }
}
