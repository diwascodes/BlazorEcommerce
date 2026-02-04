using BlazorEcommerce.Data.Models;
using BlazorEcommerce.Repository.IRepository;
using BlazorEcommerce.Utilities;
using Microsoft.AspNetCore.Components;
using Stripe.Checkout;

namespace BlazorEcommerce.Services
{
    public class PaymentService
    {
        private readonly NavigationManager _navigationManager;
        private readonly IOrderRepository _orderRepository;

        public PaymentService(NavigationManager navigationManager, IOrderRepository orderRepository)
        {
            _navigationManager = navigationManager;
            _orderRepository = orderRepository;
        }

        public Session CreateStripeCheckoutSession(OrderHeader orderHeader)
        {

            var lineItems = orderHeader.OrderDetails
                .Select(order => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmountDecimal = (decimal?)order.Price * 100,  //10.10-> 1010,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = order.ProductName
                        }

                    },
                    Quantity = order.Count
                }).ToList();

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = $"{_navigationManager.BaseUri}order/confirmation/{{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_navigationManager.BaseUri}cart",
                LineItems = lineItems,
                Mode = "payment",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return session;
        }

        public async Task<OrderHeader> CheckPaymentStatusAndUpdateOrder(string sessionId)
        {
            // Ensure we get order via repository (async)
            OrderHeader orderHeader = await _orderRepository.GetOrderBySessionIdAsync(sessionId);
            if (orderHeader == null)
            {
                return null!;
            }

            var service = new SessionService();
            // use async Get to avoid blocking thread
            var session = await service.GetAsync(sessionId);

            if (!string.IsNullOrEmpty(session?.PaymentStatus) &&
                string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                await _orderRepository.UpdateStatusAsync(orderHeader.Id, ProcessingStatus.Approved, session.PaymentIntentId);
            }
            return orderHeader;
        }
    }
}
