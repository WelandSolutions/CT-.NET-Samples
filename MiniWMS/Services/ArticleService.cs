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
using System.Collections.Generic;
using System.Linq;
using Weland.CompactTalk.Framework.OrderManagement;

namespace Weland.Ct.Api.Sample.MiniWMS.Services
{
    /// <summary>
    /// This class keeps hard coded articles and their quantities
    /// </summary>
    public class ArticleService
    {
        //Index of articles
        private readonly Dictionary<int, ArticleRecord> _articleRecordsById = new();

        public ArticleService()
        {
            GenerateArticles();
        }

        public IReadOnlyCollection<ArticleRecord> GetArticles()
        {
            return _articleRecordsById.Select(articles => articles.Value).ToList();
        }

        public bool TryGetArticle(int articleId, out ArticleRecord record)
        {
            return _articleRecordsById.TryGetValue(articleId, out record);
        }

        public void UpdateArticle(int articleId, PickOrder order)
        {
            switch (order.Mode)
            {
                case OrderMode.OUT:
                    _articleRecordsById[articleId].Quantity -= order.AckQuantity;
                    break;
                case OrderMode.IN:
                    _articleRecordsById[articleId].Quantity += order.AckQuantity;
                    break;
                case OrderMode.INV:
                    _articleRecordsById[articleId].Quantity = order.AckQuantity;
                    break;
            }
        }

        private void GenerateArticles()
        {
            //Generate 10 articles and add them to the index
            for (var i = 1; i <= 10; i++)
            {
                var articleRecord = new ArticleRecord
                {
                    ArticleNo = i.ToString(),
                    ArticleDesc = "Article " + i.ToString(),
                    Elevator = "Sim_1",
                    Id = i,
                    Quantity = 100,
                    TrayNo = 1
                };

                _articleRecordsById.Add(articleRecord.Id, articleRecord);
            }

        }

    }
}
