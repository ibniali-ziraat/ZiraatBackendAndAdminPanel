using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Orders.Services;
using ECommerce.Module.Payments.Areas.Payments.ViewModels;
using ECommerce.Module.Payments.Models;
using ECommerce.Module.Checkouts.Models;

namespace ECommerce.Module.Payments.Areas.Payments.Controllers
{
    [Area("Payments")]
    [ApiController]
    [Route("checkout")]
    [Authorize]
    
    public class CheckoutController :ControllerBase
    {
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private readonly IRepositoryWithTypedId<Checkout, Guid> _checkoutRepository;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;

        public CheckoutController(IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IRepositoryWithTypedId<Checkout, Guid> checkoutRepository,
            IOrderService orderService,
            IWorkContext workContext)
        {
            _paymentProviderRepository = paymentProviderRepository;
            _checkoutRepository = checkoutRepository;
            _orderService = orderService;
            _workContext = workContext;
        }

        [HttpGet("{checkoutId}/payment")]
        public async Task<IActionResult> Payment(Guid checkoutId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var checkout = await _checkoutRepository.Query().FirstOrDefaultAsync(x => x.Id == checkoutId);
            if (checkout == null)
            {
                return NotFound();
            }

            if (checkout.CustomerId != currentUser.Id && checkout.CreatedById != currentUser.Id)
            {
                return Forbid();
            }

            var checkoutPaymentForm = new CheckoutPaymentForm();
            checkoutPaymentForm.PaymentProviders = await _paymentProviderRepository.Query()
                .Where(x => x.IsEnabled)
                .Select(x => new PaymentProviderVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    LandingViewComponentName = x.LandingViewComponentName
                }).ToListAsync();

            checkoutPaymentForm.CheckoutId = checkoutId;
            return Ok(checkoutPaymentForm);
        }
    }
}
