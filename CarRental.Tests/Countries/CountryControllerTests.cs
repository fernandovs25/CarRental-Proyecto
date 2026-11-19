using CarRental.Controllers;
using CarRentalServices.Contracts;
using CarRentalServices.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRental.Tests.Countries
{
    public class CountryControllerTests
    {
        private readonly Mock<ICountryRepository> _repository = new();
        private readonly IsoCountriesController _controller;

        public CountryControllerTests()
        {
            _controller = new IsoCountriesController(_repository.Object);
        }

        [Fact]
        public void Get_ReturnsCountries()
        {
            var countries = new[] { new Country { CountryId = 1, CountryName = "Mexico" } };
            _repository.Setup(repository => repository.GetCountries()).Returns(countries);

            var result = Assert.IsType<OkObjectResult>(_controller.Index());

            Assert.Same(countries, result.Value);
        }

        [Fact]
        public void GetById_ReturnsCountry()
        {
            var country = new Country { CountryId = 1, CountryName = "Mexico" };
            _repository.Setup(repository => repository.GetCountryById(1)).Returns(country);

            var result = Assert.IsType<OkObjectResult>(_controller.GetCountryById(1));

            Assert.Same(country, result.Value);
        }

        [Fact]
        public void Post_CreatesCountry()
        {
            var country = new Country { CountryId = 1, CountryName = "Mexico" };
            _repository.Setup(repository => repository.InsertCountry(country)).Returns(true);

            var result = Assert.IsType<CreatedAtActionResult>(_controller.Create(country));

            Assert.Equal(nameof(IsoCountriesController.GetCountryById), result.ActionName);
            Assert.Equal(country, result.Value);
        }

        [Fact]
        public void Put_UpdatesCountry()
        {
            var country = new Country { CountryId = 1, CountryName = "Mexico" };
            _repository.Setup(repository => repository.UpdateCountry(country)).Returns(true);

            var result = Assert.IsType<NoContentResult>(_controller.Update(country));

            Assert.Equal(204, result.StatusCode);
        }
    }
}