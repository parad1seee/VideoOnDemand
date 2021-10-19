using Braintree;
using VideoOnDemand.Models.RequestModels;
using System.Threading.Tasks;

namespace VideoOnDemand.Services.Interfaces.External
{
    public interface IBraintreeService
    {
        /// <summary>
        /// Generate token that initialize client-side Braintree SDK
        /// </summary>
        /// <param name="userId">Id of the user</param>
        /// <returns>Token that initialize client-side Braintree SDK</returns>
        Task<string> GetClientTokenAsync(int userId);

        /// <summary>
        /// Create and initiate new Braintree transaction
        /// </summary>
        /// <param name="nonce">Braintree payment nonce</param>
        /// <param name="amount">Braintree payment amount</param>
        /// <returns>Object which contains created transaction or error message</returns>
        Task<Result<Transaction>> MakePaymentAsync(string nonce, decimal amount);

        /// <summary>
        /// Find Braintree customer with specified Id
        /// </summary>
        /// <param name="userId">Id of the customer to be found</param>
        /// <returns>Customer object with found customer data or null</returns>
        Task<Customer> FindCustomerByIdAsync(int userId);

        /// <summary>
        /// Create Braintree subscription by associating payment method with specified plan
        /// </summary>
        /// <param name="paymentMethodToken">Token of the Braintree payment method</param>
        /// <param name="planId">Id of the plan</param>
        /// <returns>Result of the operation of subscription creation</returns>
        Task<Result<Subscription>> CreateSubscriptionAsync(string paymentMethodToken, string planId);

        /// <summary>
        /// Process Braintree subscription-webhooks 
        /// </summary>
        /// <param name="signature">Braintree webhook signature</param>
        /// <param name="payload">Braintree webhook payload</param>
        /// <returns>Kind of the notification</returns>
        string ProcessSubscriptionWebhook(string signature, string payload);
    }
}