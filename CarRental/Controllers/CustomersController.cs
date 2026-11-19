using CarRentalServices.Contracts;
using CarRentalServices.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomersController(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var lstCustomers = _customerRepository.GetCustomers();
            return Ok(lstCustomers);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetCustomerById(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Customer id must be greater than zero.");
            }

            var customer = _customerRepository.GetCustomerById(id);
            return customer == null ? NotFound() : Ok(customer);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Customer customer)
        {
            var result = _customerRepository.InsertCustomer(customer);
            if (!result)
            {
                return Problem("Customer could not be created.");
            }

            return CreatedAtAction(
                nameof(GetCustomerById),
                new { id = customer.IdCustomer },
                customer
            );
        }

        [HttpPut]
        public IActionResult Update([FromBody] Customer customer)
        {
            if (customer.IdCustomer <= 0)
            {
                return BadRequest("Customer id must be greater than zero.");
            }

            var result = _customerRepository.UpdateCustomer(customer);
            return result ? NoContent() : NotFound();
        }
    }
}