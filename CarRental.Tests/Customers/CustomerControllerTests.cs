using CarRental.Controllers;
using CarRentalServices.Contracts;
using CarRentalServices.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRental.Tests.Customers
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerRepository> _repository = new();
        private readonly CustomersController _controller;

        public CustomerControllerTests()
        {
            _controller = new CustomersController(_repository.Object);
        }

        [Fact]
        public void Get_ReturnsCustomers()
        {
            var customers = new[] { new Customer { IdCustomer = 1, FirstName = "Ana" } };
            _repository.Setup(repository => repository.GetCustomers()).Returns(customers);

            var result = Assert.IsType<OkObjectResult>(_controller.Index());

            Assert.Same(customers, result.Value);
        }

        [Fact]
        public void GetById_ReturnsCustomer()
        {
            var customer = new Customer { IdCustomer = 1, FirstName = "Ana" };
            _repository.Setup(repository => repository.GetCustomerById(1)).Returns(customer);

            var result = Assert.IsType<OkObjectResult>(_controller.GetCustomerById(1));

            Assert.Same(customer, result.Value);
        }

        [Fact]
        public void Post_CreatesCustomer()
        {
            var customer = new Customer { IdCustomer = 1, FirstName = "Ana" };
            _repository.Setup(repository => repository.InsertCustomer(customer)).Returns(true);

            var result = Assert.IsType<CreatedAtActionResult>(_controller.Create(customer));

            Assert.Equal(nameof(CustomersController.GetCustomerById), result.ActionName);
            Assert.Equal(customer, result.Value);
        }

        [Fact]
        public void Put_UpdatesCustomer()
        {
            var customer = new Customer { IdCustomer = 1, FirstName = "Ana" };
            _repository.Setup(repository => repository.UpdateCustomer(customer)).Returns(true);

            var result = Assert.IsType<NoContentResult>(_controller.Update(customer));

            Assert.Equal(204, result.StatusCode);
        }
    }
}