using CarRentalServices.Contracts;
using CarRentalServices.Entities;
using CarRentalServices.Utilities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CarRentalServices.Repositories
{
    public class CountryRepository : ICountryRepository
    {
        private readonly IDataService _dataService;

        public CountryRepository(IDataService dataService)
        {
            _dataService = dataService;
        }

        public IEnumerable<Country> GetCountries()
        {
            var data = _dataService.ExecSP("dbo.GetCountries");
            return data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0
                ? MapCountries(data.Tables[0])
                : [];
        }

        public Country? GetCountryById(int id)
        {
            IEnumerable<SqlParameter> parameters =
            [
                SqlParameterBuilder.CreateParameter("@CountryId", SqlDbType.Int, id)
            ];
            var data = _dataService.ExecuteStoredProcedure("dbo.GetCountryById", parameters);
            return data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0
                ? MapCountries(data.Tables[0]).FirstOrDefault()
                : null;
        }

        public bool InsertCountry(Country country)
        {
            IEnumerable<SqlParameter> parameters = CreateParameters(country, includeId: false);
            var data = _dataService.ExecuteStoredProcedure("dbo.InsertCountry", parameters);
            if (data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0 &&
                data.Tables[0].Columns.Contains("newCountryId"))
            {
                country.CountryId = Convert.ToInt32(data.Tables[0].Rows[0]["newCountryId"]);
                return country.CountryId > 0;
            }

            return false;
        }

        public bool UpdateCountry(Country country)
        {
            IEnumerable<SqlParameter> parameters = CreateParameters(country, includeId: true);
            var data = _dataService.ExecuteStoredProcedure("dbo.UpdateCountry", parameters);
            return data.Tables.Count > 0 && data.Tables[0].Rows.Count > 0 &&
                data.Tables[0].Columns.Contains("RowsAffected") &&
                Convert.ToInt32(data.Tables[0].Rows[0]["RowsAffected"]) > 0;
        }

        private static List<Country> MapCountries(DataTable table)
        {
            return table.Rows.Cast<DataRow>().Select(row => new Country
            {
                CountryId = Convert.ToInt32(row["CountryId"]),
                CountryName = Convert.ToString(row["CountryName"]) ?? string.Empty,
                Iso2 = Convert.ToString(row["Iso2"]) ?? string.Empty,
                Iso3 = Convert.ToString(row["Iso3"]) ?? string.Empty
            }).ToList();
        }

        private static IEnumerable<SqlParameter> CreateParameters(Country country, bool includeId)
        {
            var parameters = new List<SqlParameter>
            {
                SqlParameterBuilder.CreateParameter("@CountryName", SqlDbType.VarChar, 100, country.CountryName),
                SqlParameterBuilder.CreateParameter("@Iso2", SqlDbType.Char, 2, country.Iso2),
                SqlParameterBuilder.CreateParameter("@Iso3", SqlDbType.Char, 3, country.Iso3)
            };

            if (includeId)
            {
                parameters.Add(SqlParameterBuilder.CreateParameter("@CountryId", SqlDbType.Int, country.CountryId));
            }

            return parameters;
        }
    }
}