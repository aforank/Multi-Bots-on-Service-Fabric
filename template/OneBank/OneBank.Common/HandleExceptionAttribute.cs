namespace OneBank.Common
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http.Filters;

    /// <summary>
    /// Defines the <see cref="HandleExceptionAttribute" />
    /// </summary>
    public class HandleExceptionAttribute : ExceptionFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HandleExceptionAttribute" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public HandleExceptionAttribute()
        {
        }

        /// <summary>
        /// Traces the exception thrown by the controller.
        /// </summary>
        /// <param name="actionExecutedContext">The context for the action.</param>
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext != null && actionExecutedContext.Exception != null)
            {
            }

            base.OnException(actionExecutedContext);

            string friendlyErrorMessage = $"Something went wrong";

            HttpResponseMessage respMessage = new HttpResponseMessage
            {
                Content = new StringContent(friendlyErrorMessage),
                ReasonPhrase = actionExecutedContext.Exception.Message.Replace(Environment.NewLine, " "),
                StatusCode = HttpStatusCode.InternalServerError
            };
        }
    }
}
