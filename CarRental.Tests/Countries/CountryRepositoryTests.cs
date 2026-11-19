using System.Data;
using CarRentalServices.Contracts;
using CarRentalServices.Entities;
using CarRentalServices.Repositories;
using Moq;

namespace CarRental.Tests.Countries;

public class CountryRepositoryTests
{
    private readonly Mock<IDataService> _dataService = new();

    [Fact]
    public void GetCountries_MapsProcedureResults()
    {
        var data = new DataSet();
        var table = data.Tables.Add();
        table.Columns.Add("CountryId", typeof(int));
        table.Columns.Add("CountryName", typeof(string));
        table.Columns.Add("Iso2", typeof(string));
        table.Columns.Add("Iso3", typeof(string));
        table.Rows.Add(1, "Mexico", "MX", "MEX");
        _dataService.Setup(service => service.ExecSP("dbo.GetCountries")).Returns(data);

        var country = Assert.Single(new CountryRepository(_dataService.Object).GetCountries());

        Assert.Equal("MEX", country.Iso3);
        Assert.Equal(1, country.CountryId);
    }

    [Fact]
    public void GetCountryById_PassesIdToProcedure()
    {
        _dataService.Setup(service => service.ExecuteStoredProcedure("dbo.GetCountryById", It.IsAny<IEnumerable<IDbDataParameter>>()))
            .Returns(new DataSet());

        Assert.Null(new CountryRepository(_dataService.Object).GetCountryById(17));

        _dataService.Verify(service => service.ExecuteStoredProcedure("dbo.GetCountryById",
            It.Is<IEnumerable<IDbDataParameter>>(parameters => parameters.Any(parameter => parameter.ParameterName == "@CountryId" && Equals(parameter.Value, 17)))), Times.Once);
    }

    [Fact]
    public void InsertCountry_DoesNotTrustIdFromRequest()
    {
        _dataService.Setup(service => service.ExecuteStoredProcedure("dbo.InsertCountry", It.IsAny<IEnumerable<IDbDataParameter>>()))
            .Returns(new DataSet());

        Assert.False(new CountryRepository(_dataService.Object).InsertCountry(new Country { CountryId = 12 }));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void UpdateCountry_UsesRowsAffected(int rowsAffected, bool expected)
    {
        var data = new DataSet();
        var table = data.Tables.Add();
        table.Columns.Add("RowsAffected", typeof(int));
        table.Rows.Add(rowsAffected);
        _dataService.Setup(service => service.ExecuteStoredProcedure("dbo.UpdateCountry", It.IsAny<IEnumerable<IDbDataParameter>>()))
            .Returns(data);

        Assert.Equal(expected, new CountryRepository(_dataService.Object).UpdateCountry(new Country { CountryId = 1 }));
    }
}