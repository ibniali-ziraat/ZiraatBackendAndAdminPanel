using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Cms.Areas.Cms.Controllers;
using ECommerce.Module.Cms.Areas.Cms.ViewModels;
using ECommerce.Module.Cms.Models;
using Xunit;

namespace ECommerce.Module.Cms.Tests.Controllers
{
    public class MenuApiControllerTests
    {
        [Fact]
        public async Task Post_CreatesMenu()
        {
            // Arrange
            var menuRepositoryMock = new Mock<IRepository<Menu>>();
            var menuItemRepositoryMock = new Mock<IRepository<MenuItem>>();

            var controller = new MenuApiController(menuRepositoryMock.Object, menuItemRepositoryMock.Object);

            var menuForm = new MenuForm
            {
                Name = "NewMenu",
                IsPublished = true
            };

            // Act
            var result = await controller.Post(menuForm);

            // Assert
            var okResult = Assert.IsType<JsonResult>(result);
            var createdMenu = Assert.IsType<Menu>(okResult.Value);
            Assert.Equal("NewMenu", createdMenu.Name);
            Assert.True(createdMenu.IsPublished);
        }
    }
}
