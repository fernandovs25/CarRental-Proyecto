using System.Data;
using CarRentalServices.Contracts;
using CarRentalServices.Entities;
using CarRentalServices.Repositories;
using Moq;

namespace CarRental.Tests.Customers;

public class CustomerRepositoryTests
{
    private readonly Mock<IDataService> _dataService = new();

    [Fact]
    public void GetCustomers_MapsProcedureResults()
    {
        var data = new DataSet();
        var table = data.Tables.Add();
        table.Columns.Add("IdCustomer", typeof(int));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));
        table.Columns.Add("SSN", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("CountryId", typeof(int));
        table.Columns.Add("Country", typeof(string));
        table.Columns.Add("Mobile", typeof(string));
        table.Columns.Add("StateName", typeof(string));
        table.Rows.Add(7, "Ana", "Lopez", "TEST-001", "ana@example.test", 2, "Mexico", "5550101", "Quintana Roo");
        _dataService.Setup(service => service.ExecSP("dbo.GetCustomers")).Returns(data);

        var customer = Assert.Single(new CustomerRepository(_dataService.Object).GetCustomers());

        Assert.Equal(7, customer.IdCustomer);
        Assert.Equal(2, customer.CountryId);
        Assert.Equal("Mexico", customer.CountryName);
    }

    [Fact]
    public void GetCustomerById_PassesIdToProcedure()
    {
        _dataService.Setup(service => service.ExecuteStoredProcedure("dbo.GetCustomerById", It.IsAny<IEnumerable<IDbDataParameter>>()))
            .Returns(new DataSet());

        Assert.Null(new CustomerRepository(_dataService.Object).GetCustomerById(17));

        _dataService.Verify(service => service.ExecuteStoredProcedure("dbo.GetCustomerById",
            It.Is<IEnumerable<IDbDataParameter>>(parameters => parameters.Any(parameter => parameter.ParameterName == "@customerId" && Equals(parameter.Value, 17)))), Times.Once);
    }

    [Fact]
    public void InsertCustomer_DoesNotTrustIdFromRequest()
    {
        _dataService.Setup(service => service.ExecuteStoredProcedure("InsertCustomer", It.IsAny<IEnumerable<IDbDataParameter>>()))
            .Returns(new DataSet());

        Assert.False(new CustomerRepository(_dataService.Object).InsertCustomer(new Customer { IdCustomer = 12 }));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void UpdateCustomer_UsesRowsAffected(int rowsAffected, bool expected)
    {
        var data = new DataSet();
        var table = data.Tables.Add();
        table.Columns.Add("RowsAffected", typeof(int));
        table.Rows.Add(rowsAffected);
        _dataService.Setup(service => service.ExecuteStoredProcedure("UpdateCustomer", It.IsAny<IEnumerable<IDbDataParameter>>()))
            .Returns(data);

        Assert.Equal(expected, new CustomerRepository(_dataService.Object).UpdateCustomer(new Customer { IdCustomer = 1 }));
    }
}