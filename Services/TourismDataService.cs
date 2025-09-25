using ExcelDataReader;
using System.Data;
using System.Text;
using TesteLLMs.Models;

public class TourismDataService
{

    private readonly string _filePath;
    public TourismDataService(string filePath)
    {
        _filePath = filePath;
    }
    public async Task<List<TourismDestination>> LoadDataset()
    {

        var destinations = new List<TourismDestination>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read))
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            DataTable dataTable = result.Tables[0];

            foreach (DataRow row in dataTable.Rows)
            {
                // Verificar se a linha não está vazia
                if (row.IsNull("Destination")) continue;

                var destination = new TourismDestination
                {
                    Destination = await SafeGetString(row, "Destination"),
                    Region = await SafeGetString(row, "Region"),
                    Country = await SafeGetString(row, "Country"),
                    Category = await SafeGetString(row, "Category"),
                    Latitude =  await SafeGetDouble(row, "Latitude"),
                    Longitude =  await SafeGetDouble(row, "Longitude"),
                    ApproximateAnnualTourists = await SafeGetString(row, "Approximate Annual Tourists"),
                    Currency = await SafeGetString(row, "Currency"),
                    MajorityReligion = await SafeGetString(row, "Majority Religion"),
                    FamousFoods = await SafeGetString(row, "Famous Foods"),
                    Language = await SafeGetString(row, "Language"),
                    BestTimeToVisit = await SafeGetString(row, "Best Time to Visit"),
                    CostOfLiving = await SafeGetString(row, "Cost of Living"),
                    Safety = await SafeGetString(row, "Safety"),
                    CulturalSignificance = await SafeGetString(row, "Cultural Significance"),
                    Description = await SafeGetString(row, "Description")
                };

                destinations.Add(destination);
            }
        }

        return destinations;
    }

    public async Task<List<TourismDestination>> SearchDestinations(string query)
    {
        var allDestinations = await LoadDataset();
        var normalizedQuery = query.ToLower();

        return allDestinations.Where(d =>
            d.Destination?.ToLower().Contains(normalizedQuery) == true ||
            d.Country?.ToLower().Contains(normalizedQuery) == true ||
            d.Region?.ToLower().Contains(normalizedQuery) == true ||
            d.Category?.ToLower().Contains(normalizedQuery) == true ||
            d.Description?.ToLower().Contains(normalizedQuery) == true
        ).ToList();
    }

    public async Task<TourismDestination> GetDestinationInfo(string destinationName)
    {
        var allDestinations = await LoadDataset();
        return allDestinations.FirstOrDefault(d =>
            d.Destination?.Equals(destinationName, StringComparison.OrdinalIgnoreCase) == true);
    }

    // Métodos auxiliares para segurança
    private async Task<string> SafeGetString(DataRow row, string columnName)
    {
        return row.Table.Columns.Contains(columnName) && !row.IsNull(columnName)
            ? row[columnName].ToString()
            : string.Empty;
    }

    private async Task<double> SafeGetDouble(DataRow row, string columnName)
    {
        if (row.Table.Columns.Contains(columnName) && !row.IsNull(columnName))
        {
            if (double.TryParse(row[columnName].ToString(), out double result))
                return result;
        }
        return 0.0;
    }
}