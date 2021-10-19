using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VideoOnDemand.Common.Constants;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Domain.Entities.Payment;
using VideoOnDemand.Models.Enums;
using VideoOnDemand.Models.Payments;
using VideoOnDemand.Models.ResponseModels;
using VideoOnDemand.Services.Interfaces.External;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.External
{
    public class StripeService : IStripeService
    {
        private IUnitOfWork _unitOfWork;
        private IConfiguration _configuration;

        public StripeService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<StripeIdResponseModel> MakePaymentAsync(int userId, string cardToken, long amount, string currency)
        {
            await EnsureCustomerCreatedAsync(userId);

            SetStripeApiKey();

            Charge charge = null;

            try
            {
                charge = await new ChargeService()
                    .CreateAsync(new ChargeCreateOptions
                    {
                        Source = cardToken,
                        Amount = amount,
                        Currency = currency
                    });
            }
            catch (StripeException ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "Stripe", ex.Message);
            }

            if (charge.FailureCode != null)
                throw new CustomException(HttpStatusCode.BadRequest, "Stripe", $"({charge.FailureCode}) {charge.FailureMessage}");

            // TODO: transaction successful - save data to DB

            return new StripeIdResponseModel { Id = charge.Id };
        }

        public async Task<StripeIdResponseModel> AddCardAsync(int userId, string cardToken)
        {
            var user = await EnsureCustomerCreatedAsync(userId);

            SetStripeApiKey();

            Card card = null;

            try
            {
                card = await new CardService()
                    .CreateAsync(user.Profile.StripeCustomerId, new CardCreateOptions { Source = cardToken });
            }
            catch (StripeException ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "Stripe", ex.Message);
            }

            return new StripeIdResponseModel { Id = card.Id };
        }

        public async Task RemoveCardAsync(int userId, string cardId)
        {
            var user = await EnsureCustomerCreatedAsync(userId);

            SetStripeApiKey();

            Card card = null;

            try
            {
                card = await new CardService()
                    .DeleteAsync(user.Profile.StripeCustomerId, cardId);
            }
            catch (StripeException ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "Stripe", ex.Message);
            }
        }

        public async Task<List<StripeIdResponseModel>> GetAllCardsAsync(int userId)
        {
            var user = await EnsureCustomerCreatedAsync(userId);

            SetStripeApiKey();

            StripeList<Card> cards = null;

            try
            {
                cards = await new CardService()
                    .ListAsync(user.Profile.StripeCustomerId);
            }
            catch (StripeException ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, "Stripe", ex.Message);
            }

            return cards.Select(i => new StripeIdResponseModel { Id = i.Id }).ToList();
        }

        public async Task UpdateCustomerPaymentMethod(string paymentMethodToken, string customerId)
        {
            SetStripeApiKey();

            var options = new CustomerUpdateOptions
            {
                Source = paymentMethodToken
            };

            var customer = new CustomerService()
                .Update(customerId, options);
        }

        public async Task ProcessWebhook(HttpContext httpContext, HttpRequest httpRequest)
        {
            string json = new StreamReader(httpContext.Request.Body).ReadToEnd();

            var stripeEvent = EventUtility.ConstructEvent(json, httpRequest.Headers["Stripe-Signature"], _configuration["Stripe:WebhookSecret"]);

            switch (stripeEvent.Type)
            {
                case StripeWebhookType.ChargeSucceeded:
                    // logic
                    break;
                case StripeWebhookType.ChargeFailed:
                    // logic
                    break;
                case StripeWebhookType.ChargePending:
                    // logic
                    break;
                case StripeWebhookType.ChargeRefunded:
                    // logic
                    break;
                case StripeWebhookType.ChargeUpdated:
                    // logic
                    break;

                case StripeWebhookType.SubscriptionCreated:
                    {
                        var newSubscription = stripeEvent.Data.Object as Subscription;
                        var subscriptionEntity = new StripeSubscription
                        {
                            CreatedAt = newSubscription.Created,
                            EndedAt = newSubscription.EndedAt,
                            TrialEnd = newSubscription.TrialEnd,
                            SubscriptionId = newSubscription.Id,
                            UserId = _unitOfWork.Repository<ApplicationUser>().Get(x => x.Profile.StripeCustomerId == newSubscription.CustomerId).TagWith(nameof(ProcessWebhook) + "_GetUserId").First().Id,
                            Status = ParseSubscriptionStatus(newSubscription.Status)
                        };

                        _unitOfWork.Repository<StripeSubscription>().Insert(subscriptionEntity);

                        break;
                    }
                case StripeWebhookType.SubscriptionDeleted:
                    {
                        var subscription = stripeEvent.Data.Object as Subscription;
                        var subscriptionEntity = _unitOfWork.Repository<StripeSubscription>().Find(x => x.SubscriptionId == subscription.Id);

                        if (subscriptionEntity != null)
                        {
                            subscriptionEntity.Status = ParseSubscriptionStatus(subscription.Status);
                            _unitOfWork.Repository<StripeSubscription>().Update(subscriptionEntity);
                        }

                        break;
                    }
                case StripeWebhookType.SubscriptionUpdated:
                    {
                        var subscription = stripeEvent.Data.Object as Subscription;
                        var subscriptionEntity = _unitOfWork.Repository<StripeSubscription>().Find(x => x.SubscriptionId == subscription.Id);

                        if (subscriptionEntity != null)
                        {
                            subscriptionEntity.Status = ParseSubscriptionStatus(subscription.Status);
                            _unitOfWork.Repository<StripeSubscription>().Update(subscriptionEntity);
                        }

                        break;
                    }
                case StripeWebhookType.SubscriptionTrialEnd:
                    // logic
                    break;

                case StripeWebhookType.InvoiceCreated:
                    // logic
                    break;
                case StripeWebhookType.InvoiceDeleted:
                    // logic
                    break;
                case StripeWebhookType.InvoicePaymentSucceeded:
                    // logic
                    break;
                case StripeWebhookType.InvoicePaymentFailed:
                    // logic
                    break;
                    // all webhooks types here: https://stripe.com/docs/api/events/types
            }

            _unitOfWork.SaveChanges();
        }

        // create subscription
        public async Task<StripeIdResponseModel> CreateSubscription(string planId, int userId)
        {
            var user = await EnsureCustomerCreatedAsync(userId);

            SetStripeApiKey();

            var subscription = await FindSubscriptionByPlan(planId, user.Profile.StripeCustomerId);

            if (subscription != null)
                return new StripeIdResponseModel { Id = subscription.Id };

            var subscriptionCreateOptions = new SubscriptionCreateOptions
            {
                Customer = user.Profile.StripeCustomerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions {Plan = planId}
                },
                TrialPeriodDays = 3
            };

            var newSubscription = await new SubscriptionService()
                .CreateAsync(subscriptionCreateOptions);

            return new StripeIdResponseModel
            {
                Id = newSubscription.Id
            };
        }

        // cancel subscription
        public async Task<StripeIdResponseModel> CancelSubscription(string subscriptionId, int userId)
        {
            var user = await EnsureCustomerCreatedAsync(userId);

            SetStripeApiKey();

            var service = new SubscriptionService();

            Subscription subscription = null;
            try
            {
                subscription = await service.GetAsync(subscriptionId);

                if (subscription.CustomerId != user.Profile.StripeCustomerId)
                    throw new CustomException(HttpStatusCode.BadRequest, "subscriptionId", "Invalid subscription id");

                subscription = await service.CancelAsync(subscriptionId, null);
            }
            catch (StripeException ex)
            {
                throw new CustomException(HttpStatusCode.BadRequest, ex.StripeError?.Param, ex.StripeError?.Message);
            }

            return new StripeIdResponseModel
            {
                Id = subscription?.Id
            };
        }

        // find subscription by plan id
        public async Task<Subscription> FindSubscriptionByPlan(string planId, string customerId)
        {
            var service = new SubscriptionService();

            var subscriptionListOptions = new SubscriptionListOptions
            {
                Customer = customerId,
                Plan = planId
            };

            var subscriptions = await service.ListAsync(subscriptionListOptions);

            return subscriptions.FirstOrDefault();
        }

        private void SetStripeApiKey()
        {
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        private async Task<ApplicationUser> EnsureCustomerCreatedAsync(int userId)
        {
            var user = await GetUserAsync(userId);

            if (string.IsNullOrEmpty(user.Profile.StripeCustomerId))
                await CreateCustomerAsync(user);

            return user;
        }

        private async Task<ApplicationUser> GetUserAsync(int userId)
        {
            if (userId <= 0)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", "User Id is invalid");

            var user = await _unitOfWork.Repository<ApplicationUser>()
                .Get(i => i.Id == userId && i.IsActive && !i.IsDeleted && i.PhoneNumberConfirmed)
                .TagWith(nameof(GetUserAsync) + "_GetUser")
                .Include(i => i.Profile)
                .FirstOrDefaultAsync();

            if (user == null || user.Profile == null)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", "User Id is invalid");

            return user;
        }

        private async Task CreateCustomerAsync(ApplicationUser user)
        {
            SetStripeApiKey();

            Customer customer = null;

            try
            {
                customer = await new CustomerService()
                    .CreateAsync(new CustomerCreateOptions { Description = $"Customer for user with id {user.Id}", Email = user.Email });
            }
            catch (StripeException ex)
            {
                throw;
            }

            user.Profile.StripeCustomerId = customer.Id;

            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();
        }

        // get subscription status from string
        private static StripeSubscriptionStatus ParseSubscriptionStatus(string status)
        {
            return status switch
            {
                "incomplete" => StripeSubscriptionStatus.Incomplete,
                "incomplete_expired" => StripeSubscriptionStatus.IncompleteExpired,
                "trialing" => StripeSubscriptionStatus.Trialing,
                "active" => StripeSubscriptionStatus.Active,
                "past_due" => StripeSubscriptionStatus.PastDue,
                "canceled" => StripeSubscriptionStatus.Canceled,
                "unpaid" => StripeSubscriptionStatus.Unpaid,
                _ => StripeSubscriptionStatus.Canceled,
            };
        }
    }
}
