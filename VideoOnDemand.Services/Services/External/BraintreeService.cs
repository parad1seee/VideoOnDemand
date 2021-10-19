using Braintree;
using Braintree.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VideoOnDemand.Common.Exceptions;
using VideoOnDemand.Common.Extensions;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.Domain.Entities.Identity;
using VideoOnDemand.Services.Interfaces.External;
using System.Net;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Services.External
{
    public class BraintreeService : IBraintreeService
    {
        private IBraintreeGateway _braintreeGateway;
        private IUnitOfWork _unitOfWork;

        public BraintreeService(IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _braintreeGateway = new BraintreeGateway
            {
                Environment = Braintree.Environment.ParseEnvironment(configuration["Braintree:Environment"]),
                MerchantId = configuration["Braintree:MerchantId"],
                PublicKey = configuration["Braintree:PublicKey"],
                PrivateKey = configuration["Braintree:PrivateKey"]
            };

            _unitOfWork = unitOfWork;
        }

        public async Task<string> GetClientTokenAsync(int userId)
        {
            var user = await GetUserAsync(userId);

            if (string.IsNullOrEmpty(user.Profile.BraintreeCustomerId))
                await CreateCustomerAsync(userId);

            return await _braintreeGateway
                .ClientToken
                .GenerateAsync(new ClientTokenRequest
                {
                    CustomerId = user.Profile.BraintreeCustomerId
                });
        }

        public async Task<Result<Transaction>> MakePaymentAsync(string nonce, decimal amount)
        {
            Result<Transaction> transactionResult = null;

            try
            {
                transactionResult = await _braintreeGateway
                    .Transaction
                    .SaleAsync(new TransactionRequest
                    {
                        Amount = amount,
                        PaymentMethodNonce = nonce,
                        Options = new TransactionOptionsRequest { SubmitForSettlement = true }
                    });
            }
            catch (BraintreeException ex)
            {
                throw;
            }

            if (!transactionResult.IsSuccess())
                transactionResult.ThrowArgExOnOperationFailure();

            return transactionResult;
        }

        public async Task<Customer> FindCustomerByIdAsync(int userId)
        {
            var user = await GetUserAsync(userId);

            if (user.Profile.BraintreeCustomerId == null)
                await CreateCustomerAsync(userId);

            return await _braintreeGateway.Customer.FindAsync(user.Profile.BraintreeCustomerId);
        }

        public async Task<Result<Subscription>> CreateSubscriptionAsync(string paymentMethodToken, string planId)
        {
            Result<Subscription> subscriptionResult = null;

            try
            {
                subscriptionResult = await _braintreeGateway
                    .Subscription
                    .CreateAsync(new SubscriptionRequest
                    {
                        PaymentMethodToken = paymentMethodToken,
                        PlanId = planId
                    });
            }
            catch (BraintreeException ex)
            {
                throw;
            }

            if (!subscriptionResult.IsSuccess())
                subscriptionResult.ThrowArgExOnOperationFailure();

            return subscriptionResult;
        }

        public string ProcessSubscriptionWebhook(string signature, string payload)
        {
            var webhookNotification = _braintreeGateway
                .WebhookNotification
                .Parse(signature, payload);

            var kind = webhookNotification.Kind;

            // if (kind == WebhookKind.SUBSCRIPTION_WENT_ACTIVE)
            //     // logic
            // else if (kind == WebhookKind.SUBSCRIPTION_WENT_PAST_DUE)
            //     // logic
            // else if ...
            // 
            // all webhooks notification kinds here: https://developers.braintreepayments.com/reference/general/webhooks/subscription/dotnet#subscription 

            return kind.ToString();
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

        private async Task<ApplicationUser> CreateCustomerAsync(int userId)
        {
            var user = await GetUserAsync(userId);

            if (user.Profile.BraintreeCustomerId != null)
                throw new CustomException(HttpStatusCode.BadRequest, "userId", "User already has Braintree CustomerId");

            Result<Customer> customerCreationResult = null;

            try
            {
                customerCreationResult = await _braintreeGateway.Customer.CreateAsync(new CustomerRequest { Phone = user.PhoneNumber });
            }
            catch (BraintreeException ex)
            {
                throw;
            }

            if (!customerCreationResult.IsSuccess())
                customerCreationResult.ThrowArgExOnOperationFailure();

            user.Profile.BraintreeCustomerId = customerCreationResult.Target.Id;

            _unitOfWork.Repository<ApplicationUser>().Update(user);
            _unitOfWork.SaveChanges();

            return user;
        }
    }
}