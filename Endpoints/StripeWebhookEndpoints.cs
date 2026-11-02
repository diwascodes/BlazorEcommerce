using BlazorEcommerce.Repository.IRepository;
using BlazorEcommerce.Utilities;
using Stripe;
using Stripe.Checkout;

namespace BlazorEcommerce.Endpoints
{
    public static class StripeWebhookEndpoints
    {
        public static IEndpointRouteBuilder MapStripeWebhookEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/api/stripe/webhook", async (
                HttpRequest request,
                IOrderRepository orderRepository,
                IShoppingCartRepository cartRepository,
                IConfiguration configuration,
                ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("StripeWebhook");
                string json;
                using (var reader = new StreamReader(request.Body))
                {
                    json = await reader.ReadToEndAsync();
                }

                var webhookSecret = configuration["StripeWebhookSecret"];
                var stripeSignature = request.Headers["Stripe-Signature"];

                Event stripeEvent;
                try
                {
                    if (!string.IsNullOrEmpty(webhookSecret))
                    {
                        stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
                    }
                    else
                    {
                        logger.LogWarning("StripeWebhookSecret is not configured. Parsing webhook event without signature verification.");
                        stripeEvent = EventUtility.ParseEvent(json);
                    }
                }
                catch (StripeException ex)
                {
                    logger.LogError(ex, "Stripe webhook signature verification failed: {Message}", ex.Message);
                    return Results.BadRequest(new { error = "Webhook signature verification failed" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading Stripe webhook payload.");
                    return Results.BadRequest(new { error = "Invalid payload" });
                }

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    if (stripeEvent.Data.Object is Session session)
                    {
                        if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                        {
                            var orderHeader = await orderRepository.GetOrderBySessionIdAsync(session.Id);
                            if (orderHeader != null)
                            {
                                if (orderHeader.Status != ProcessingStatus.Approved)
                                {
                                    await orderRepository.UpdateStatusAsync(orderHeader.Id, ProcessingStatus.Approved, session.PaymentIntentId);
                                    await cartRepository.ClearCartAsync(orderHeader.UserId);
                                    logger.LogInformation("Order #{OrderId} confirmed as Approved via Stripe webhook.", orderHeader.Id);
                                }
                                else
                                {
                                    logger.LogInformation("Order #{OrderId} already marked Approved.", orderHeader.Id);
                                }
                            }
                            else
                            {
                                logger.LogWarning("No OrderHeader found for Stripe Session ID: {SessionId}", session.Id);
                            }
                        }
                    }
                }

                return Results.Ok();
            })
            .DisableAntiforgery();

            return endpoints;
        }
    }
}
