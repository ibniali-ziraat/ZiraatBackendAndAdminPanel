using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Web.SmartTable;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Pricing.Areas.Pricing.Controllers;
using ECommerce.Module.Pricing.Areas.Pricing.ViewModels;
using ECommerce.Module.Pricing.Models;
using Xunit;

namespace ECommerce.Module.Pricing.Tests.Controllers
{
    public class CartRuleApiControllerTests
    {
        [Fact]
        public async Task List_ReturnsJsonResult()
        {
            // Arrange
            var cartRuleRepositoryMock = new Mock<IRepository<CartRule>>();
            cartRuleRepositoryMock.Setup(x => x.Query()).Returns(new List<CartRule>().AsQueryable());
            var controller = CreateController(cartRuleRepositoryMock);
            var smartTableParam = new SmartTableParam()
            {
                Pagination = new Pagination() { Start = 1, TotalItemCount = 1, Number = 1, NumberOfPages = 1 },
                Search = new Search() { },
                Sort = new Sort() { }
            };
            
            // Act
            var result = await controller.List(smartTableParam);
            
            // Assert
            Assert.IsType<JsonResult>(result);
        }

        [Fact]
        public async Task Post_CreatesNewCartRule_WhenModelStateIsValid()
        {
            // Arrange
            var cartRuleRepositoryMock = new Mock<IRepository<CartRule>>();
            var controller = CreateController(cartRuleRepositoryMock);
            var model = new CartRuleForm
            {
                Id = 1,
                Name = "Test Cart Rule",
                IsActive = true,
                StartOn = DateTime.Now,
                EndOn = DateTime.Now.AddDays(7),
                IsCouponRequired = true,
                RuleToApply = "Coupon",
                DiscountAmount = 10,
                DiscountStep = 2,
                MaxDiscountAmount = 50,
                UsageLimitPerCoupon = 100,
                UsageLimitPerCustomer = 1,
                Products = new List<CartRuleProductVm>()
            };
            
            // Act
            var result = await controller.Post(model);
            
            // Assert
            Assert.IsType<OkObjectResult>(result);
            cartRuleRepositoryMock.Verify(repo => repo.Add(It.IsAny<CartRule>()), Times.Once);
            cartRuleRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        private static CartRuleApiController CreateController(Mock<IRepository<CartRule>> cartRuleRepositoryMock, User currentUser = null)
        {
            var workContextMock = new Mock<IWorkContext>();
            workContextMock.Setup(x => x.GetCurrentUser()).ReturnsAsync(currentUser ?? new User { Id = 1, FullName = "Admin" });

            var controller = new CartRuleApiController(cartRuleRepositoryMock.Object, workContextMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Role, "admin")
                    }, "Test"))
                }
            };

            return controller;
        }
    }
}
