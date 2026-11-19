using CarRentalServices.Contracts;
using CarRentalServices.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IsoCountriesController : ControllerBase
    {
        private readonly ICountryRepository _countryRepository;

        public IsoCountriesController(ICountryRepository countryRepository)
        {
            _countryRepository = countryRepository;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok(_countryRepository.GetCountries());
        }

        [HttpGet("{id:int}")]
        public IActionResult GetCountryById(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Country id must be greater than zero.");
            }

            var country = _countryRepository.GetCountryById(id);
            return country == null ? NotFound() : Ok(country);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Country country)
        {
            if (!_countryRepository.InsertCountry(country))
            {
                return Problem("Country could not be created.");
            }

            return CreatedAtAction(nameof(GetCountryById), new { id = country.CountryId }, country);
        }

        [HttpPut]
        public IActionResult Update([FromBody] Country country)
        {
            if (country.CountryId <= 0)
            {
                return BadRequest("Country id must be greater than zero.");
            }

            return _countryRepository.UpdateCountry(country) ? NoContent() : NotFound();
        }
    }
}