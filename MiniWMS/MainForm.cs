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
using Weland.Ct.Api.Sample.MiniWMS.Models;
using Weland.Ct.Api.Sample.MiniWMS.Services;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Weland.CompactTalk;
using Weland.CompactTalk.Framework.OrderManagement;

namespace Weland.Ct.Api.Sample.MiniWMS
{
    public partial class MainForm : Form
    {

        private readonly ArticleService _articleService = new ArticleService();
        private readonly OrderService _orderService;

        public MainForm()
        {
            InitializeComponent();

            //Disable acknowledge button until we have a selected row in order view
            buttonExtAck.Enabled = false;
            //Disable create order button until we have a selected row in article view
            buttonCreateOrder.Enabled = false;

            _orderService = new OrderService(_articleService);
            InitializeOrderService();
            PopulateArticles();
            PopulateOrders();
        }

        private void InitializeOrderService()
        {
            try
            {
                _orderService.Initialize(this);
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to initialize order service");
                throw e;
            }

            _orderService.OnOrderStatusChanged += new OrderStatusChanged(OnOrderStatusChangedEvent);
            _orderService.OnQueueChanged += new QueueChanged(OnQueueChangedEvent);
        }

        private void PopulateArticles()
        {
            var articles = _articleService.GetArticles();
            foreach (var article in articles)
            {
                ListViewItem lvItem = listViewArticles.Items.Add(article.ArticleNo);
                lvItem.SubItems.Add(article.ArticleDesc);
                lvItem.SubItems.Add(article.Quantity.ToString());
                lvItem.SubItems.Add(article.Elevator);
                lvItem.SubItems.Add(article.TrayNo.ToString());
                lvItem.Tag = article.Id;
            }
        }

        private void PopulateOrders()
        {
            var orders = _orderService.GetOrders();
            foreach (var order in orders)
            {
                ListViewItem lvItem = listViewOrders.Items.Add(order.ArticleNo);
                lvItem.SubItems.Add(order.ArticleDesc);
                lvItem.SubItems.Add(order.Quantity.ToString());
                lvItem.SubItems.Add(order.Operation);
                lvItem.SubItems.Add(order.Elevator);
                lvItem.SubItems.Add(order.TrayNo.ToString());
                lvItem.SubItems.Add(order.ServiceOpening.ToString());
                lvItem.SubItems.Add(order.CTStatus);
                lvItem.Tag = order.CTOrderId;
            }
        }

        private void OnOrderStatusChangedEvent(CTOrderStatusChangeEvent evt)
        {
            var orderItem = listViewOrders.Items
                .Cast<ListViewItem>()
                .Where(item => (int)item.Tag == evt.Order.Id)
                .FirstOrDefault();

            if (orderItem != null && _orderService.TryGetOrder(evt.Order.Id, out var order))
            {
                // Update order in order view with new status
                orderItem.SubItems[7].Text = evt.Order.Status.ToString();

                // If the order is done. Update article total quantity
                if (evt.Order.Status == OrderStatus.TaskDone)
                {
                    var articleItem = listViewArticles.Items
                        .Cast<ListViewItem>()
                        .Where(item => (int)item.Tag == order.ArticleId)
                        .FirstOrDefault();
                    if (articleItem != null && _articleService.TryGetArticle(order.ArticleId, out var article))
                    {
                        articleItem.SubItems[2].Text = article.Quantity.ToString();
                    }
                }
            }
        }

        private void OnQueueChangedEvent(CTQueueChangeEvent evt)
        {
            // If order is deleted, remove it from the order view
            if (evt.ChangeType == OrderQueueChangeType.OrderDeleted)
            {

                var orderItem = listViewOrders.Items
                    .Cast<ListViewItem>()
                    .Where(item => (int)item.Tag == evt.Order.Id)
                    .FirstOrDefault();

                if (orderItem != null)
                {
                    listViewOrders.Items.Remove(orderItem);
                }
            }
        }

        private void buttonCreateOrder_Click(object sender, EventArgs e)
        {
            CreateOrderDlg dlg = new CreateOrderDlg();

            //Open the CreateOrderDlg to enter the pick order information
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (!_articleService.TryGetArticle((int)listViewArticles.SelectedItems[0].Tag, out var article))
                    {
                        throw new Exception("Article not found");
                    }

                    OrderRecord order = new OrderRecord
                    {
                        ArticleId = article.Id,
                        ArticleNo = article.ArticleNo,
                        ArticleDesc = article.ArticleDesc,
                        Elevator = article.Elevator,
                        TrayNo = article.TrayNo,
                        Quantity = dlg.Quantity,
                        ServiceOpening = dlg.ServiceOpening,
                        Operation = dlg.Operation
                    };

                    // Create the order in Compact Talk
                    var createdOrder = _orderService.CreateOrder(order);

                    ListViewItem lvItem = listViewOrders.Items.Add(createdOrder.ArticleNo);
                    lvItem.SubItems.Add(createdOrder.ArticleDesc);
                    lvItem.SubItems.Add(createdOrder.Quantity.ToString());
                    lvItem.SubItems.Add(createdOrder.Operation);
                    lvItem.SubItems.Add(createdOrder.Elevator);
                    lvItem.SubItems.Add(createdOrder.TrayNo.ToString());
                    lvItem.SubItems.Add(createdOrder.ServiceOpening.ToString());
                    lvItem.SubItems.Add(createdOrder.CTStatus);
                    lvItem.Tag = createdOrder.CTOrderId;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to add order to Compact Talk\n\n" + ex.Message);
                    return;
                }
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _orderService.OnOrderStatusChanged -= new OrderStatusChanged(OnOrderStatusChangedEvent);
            _orderService.OnQueueChanged -= new QueueChanged(OnQueueChangedEvent);
            _orderService.ShutDown();
        }

        private void listViewOrders_SelectedIndexChanged(object sender, EventArgs e)
        {
            //If no order selected in the order view, disable the ack button and empty the quantity field
            if (listViewOrders.SelectedIndices.Count < 1)
            {
                buttonExtAck.Enabled = false;
                textBoxQuantity.Text = "";
                return;
            }

            //If an order is selected in the order view, enable the ack button and 
            //fill the quantity field with the quantity of the order

            buttonExtAck.Enabled = true;

            int orderId = (int)listViewOrders.SelectedItems[0].Tag;

            if (_orderService.TryGetOrder(orderId, out var order))
                textBoxQuantity.Text = order.Quantity.ToString();
        }

        private void buttonExtAck_Click(object sender, EventArgs e)
        {
            int orderId = (int)listViewOrders.SelectedItems[0].Tag;

            try
            {
                // Acknowledge order in Compact Talk
                _orderService.AcknowledgeOrder(orderId, float.Parse(textBoxQuantity.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to acknowledge order. Error: " + ex.Message);
            }
        }
        private void listViewArticles_SelectedIndexChanged(object sender, EventArgs e)
        {
            //If no article selected in the article view, disable the create order button
            if (listViewArticles.SelectedIndices.Count < 1)
            {
                buttonCreateOrder.Enabled = false;
                return;
            }

            //If an article is selected in the article view, enable the create order button
            buttonCreateOrder.Enabled = true;
        }
    }
}
