﻿using Megumirai.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;
using System.Linq.Expressions;
using Microsoft.Ajax.Utilities;
using System.Security.Cryptography.X509Certificates;

namespace Megumirai.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        // GET: Order
        public ActionResult OrderAdd()
        {
            Session["customerid"] = 1;
            int subPrice = 0;
            int tax = 0;
            int totalPrice = 0;

            using (var db = new Database1Entities())
            {
                var carts = db.Carts.ToList();
                foreach (var i in carts)
                {
                    subPrice += i.Price;
                }
                tax = (int)Math.Truncate(subPrice * 0.08);
                totalPrice = subPrice + tax;

                var order = new Order
                {
                    CustomerId = (int)Session["customerid"],
                    SubPrice = subPrice,
                    Tax = tax,
                    TotalPrice = totalPrice,
                    OrderDate = DateTime.Now
                };
                db.Orders.Add(order);
                db.SaveChanges();

                int lastOrderId = db.Orders.Max(model => model.OrderId);
                int num = 1;

                foreach (var i in carts)
                {
                    var orderdetail = new OrderMix
                    {
                        OrderId = lastOrderId,
                        OrderDetailId = num,
                        ItemId = i.ItemId,
                        ItemName = i.ItemName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        DeliveryDate = DateTime.Parse(i.DeliveryDate),
                        Status = "未出荷",
                        CustomerId = (int)Session["customerid"],
                        SubPrice = order.SubPrice,
                        Tax = order.Tax,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate
                    };
                    db.OrderMixes.Add(orderdetail);
                    var u = db.Carts.Find(i.CartId);
                    db.Carts.Remove(u);
                    db.SaveChanges();
                    num += 1;
                }

                var orderList = db.Orders.Find(lastOrderId);
                ViewBag.Order = orderList;

                var ItemList = db.OrderMixes.ToList();
                var dlist = new List<OrderMixViewModel>();
                foreach (var item in ItemList)
                {
                    if (orderList.OrderId == item.OrderId)
                    {
                        var e = new OrderMixViewModel(item);
                        dlist.Add(e);
                    }
                }
                return View(dlist);
            }
        }
        public ActionResult OrderConfirm()
        {
            var salmons = new List<ConfirmViewModel>();
            using (var db = new Database1Entities())
            {
                //注文番号
                var order = db.Orders.ToList();
                ViewBag.order = order;

                //明細の情報を入れる
                var ItemList = db.OrderMixes.ToList();
                var dlist = new List<OrderMixViewModel>();
                foreach (var item in ItemList)
                {
                    var e = new OrderMixViewModel(item);
                    dlist.Add(e);
                }

                //ボタンの表示
                int num1 = 1;
                foreach (var i in order)
                {
                    int num2 = 0;
                    for (int n = 0; n < dlist.Count(); n++)
                    {
                        if (i.OrderId == dlist[n].OrderId)
                        {
                            if (dlist[n].Status != "未出荷")
                            {
                                num2 += 1;
                            }
                        }
                    }
                    salmons.Add(new ConfirmViewModel { No = num1, Check = num2 });
                    num1 += 1;
                }
                ViewBag.Button = salmons;
                return View(dlist);
            };
        }

        public ActionResult OrderCancelCheck(int orderid)
        {
            using (var db = new Database1Entities())
            {
                ViewBag.OrderId = orderid;
                var ItemList = db.OrderMixes.ToList();
                var dlist = new List<OrderMixViewModel>();
                foreach (var item in ItemList)
                {
                    if (orderid == item.OrderId)
                    {
                        var e = new OrderMixViewModel(item);
                        dlist.Add(e);
                    }
                }
                return View(dlist);
            }
        }

        public ActionResult OrderCancel(int orderid)
        {
            using (var db = new Database1Entities())
            {
                var ItemList = db.OrderMixes.ToList();
                var dlist = new List<OrderMixViewModel>();
                foreach (var item in ItemList)
                {
                    if (orderid == item.OrderId)
                    {
                        var e = new OrderMixViewModel(item);
                        var ul = db.OrderMixes.Find(item.OrderMixId);
                        ul.Status = "キャンセル";
                        db.SaveChanges();
                    }
                }
            }
            return Redirect("OrderConfirm");
        }

        // GET: Order
        [Authorize]
        public ActionResult OrderSearch()
        {
            return View();
        }
        public ActionResult OrderSearchResult(OrderMix model, DateTime? deliveryFrom, DateTime? deliveryTo, DateTime? orderFrom, DateTime? orderTo, string status)
        {

            using (var db = new Database1Entities())
            {

                var x = db.OrderMixes
                   .WhereIf(model.OrderId != 0, e => e.OrderId >= model.OrderId)
                   .WhereIf(model.CustomerId != 0, e => e.CustomerId >= model.CustomerId)
                   .WhereIf(deliveryTo != null, e => e.DeliveryDate <= deliveryTo)
                   .WhereIf(deliveryTo != null, e => e.DeliveryDate <= deliveryTo)
                   .WhereIf(orderFrom != null, e => e.OrderDate >= orderFrom)
                   .WhereIf(orderFrom != null, e => e.OrderDate >= orderFrom)
                   .WhereIf(status == "出荷済", e => e.Status == "出荷済")
                   .WhereIf(status == "未出荷", e => e.Status == "未出荷")
                   .WhereIf(status == "キャンセル", e => e.Status == "キャンセル")
                   .WhereIf(status == "全て", e => e.Status == "出荷済" | e.Status == "未出荷" | e.Status == "キャンセル")
                    .ToList();

                //検索条件を結果に渡す。                                                                    

                ViewBag.customerId = model.CustomerId;
                ViewBag.orderNo = model.OrderId;
                ViewBag.deliveryFrom = deliveryFrom;
                ViewBag.deliveryTo = deliveryTo;
                ViewBag.orderFrom = orderFrom;
                ViewBag.orderTo = orderTo;
                ViewBag.status = status;


                Session["Search"] = x;


                return View(x);

            }
        }
        public ActionResult OrderUpdateInput(OrderMix om)
        {
            using (var db = new Database1Entities())
            {
                var p = db.OrderMixes.Find(om.OrderMixId);
                Session["Update"] = p;
                return View(Session["Update"]);
            }
        }
        public ActionResult OrderUpdateX(OrderMix model, string status)
        {
            using (var db = new Database1Entities())
            {
                var p = db.OrderMixes.Find(model.OrderMixId);
                var q = db.Items.Find(p.ItemId);
                if (p.Status != status)
                {
                    if (status == "未出荷")
                    {
                        q.Stock = q.Stock + p.Quantity;
                        db.SaveChanges();
                        p.Status = status;
                        db.SaveChanges();
                    }
                    else if (status == "出荷済")
                    {
                        if (q.Stock >= p.Quantity)
                        {
                            q.Stock = q.Stock - p.Quantity;
                            db.SaveChanges();
                            p.Status = status;
                            db.SaveChanges();
                        }
                        else if (q.Stock < p.Quantity)
                        {
                            ViewBag.message = "在庫数量が不足しています。";
                            return View("OrderUpdateInput", Session["Update"]);
                        }
                    }
                }
                return View("OrderUpdate", p);
            }
        }
        public ActionResult SearchBack()
        {
            var u = Session["Search"];

            return View("OrderSearchResult", u);
        }
    }
    public static class LinqExtentions
    {
        public static IQueryable<TSource> WhereIf<TSource>
                                    (this IQueryable<TSource> Source, bool Condition, Expression<Func<TSource, bool>> Predicate)
        {
            if (Condition)
                return Source.Where(Predicate);
            else
                return Source;
        }
    }
}