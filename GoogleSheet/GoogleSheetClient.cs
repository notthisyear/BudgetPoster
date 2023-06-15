using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Google.Apis.Sheets.v4.Data;
using BudgetPoster.Common;

namespace BudgetPoster.GoogleSheet
{
    internal class GoogleSheetClient
    {
        private SheetsService? _sheetService;

        private readonly string _credentialsPath;
        private readonly string _accountName;

        public GoogleSheetClient(string credentialsPath, string accountName)
        {
            _credentialsPath = credentialsPath;
            _accountName = accountName;
        }

        public async Task Initialize(string serviceName)
        {
            var localFileStorePath = Path.GetDirectoryName(_credentialsPath);
            using var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read);
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { SheetsService.Scope.Spreadsheets },
                _accountName,
                CancellationToken.None,
                new FileDataStore(localFileStorePath, fullPath: true)
            );

            _sheetService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = serviceName
            });
        }

        public async Task<List<SheetEntry>> GetDataInSpecificSheet(string mainSheetId, int sheetId, bool firstColumnIsLabel, (int start, int end) columnRange, (int start, int end) rowRange)
        {
            List<SheetEntry> result = new();
            if (_sheetService == default)
                return result;

            var filterRequest = new GetSpreadsheetByDataFilterRequest()
            {
                DataFilters = new List<DataFilter>()
                {
                    new()
                    {
                        GridRange = new()
                        {
                            StartColumnIndex = columnRange.start,
                            EndColumnIndex = columnRange.end,
                            StartRowIndex = rowRange.start,
                            EndRowIndex = rowRange.end,
                            SheetId = sheetId
                        }
                    }
                },
                IncludeGridData = true
            };

            var request = _sheetService.Spreadsheets.GetByDataFilter(filterRequest, mainSheetId);
            var response = await request.ExecuteAsync();

            if (response == default || response.Sheets.First().Data == default || response.Sheets.First().Data.First() == default)
                return result;

            var rowData = response.Sheets.First().Data.First().RowData;
            foreach (var data in rowData)
            {
                var valueIdx = 0;
                var entryName = string.Empty;
                var currentValueList = new List<SheetValue>();
                if (firstColumnIsLabel && data.Values.Count > 1)
                {
                    entryName = data.Values.First().FormattedValue;
                    if (string.IsNullOrEmpty(entryName))
                        continue;
                    valueIdx++;
                }

                for (var i = valueIdx; i < data.Values.Count; i++)
                {
                    var currentValue = data.Values[i].EffectiveValue;
                    if (currentValue == default)
                        continue;

                    if (currentValue.NumberValue != default)
                        currentValueList.Add(SheetValue.FromNumber((double)currentValue.NumberValue!));
                    else if (!string.IsNullOrWhiteSpace(currentValue.StringValue))
                        currentValueList.Add(SheetValue.FromString(currentValue.StringValue));
                    else
                        LoggerType.GoogleCommunication.Log(LoggingLevel.Warning, "Got value that is neither string nor number. Skipping.");
                }

                if (currentValueList.Any())
                    result.Add(new(entryName, currentValueList));
            }

            return result;
        }

        public async Task<int> GetIdOfSpecificSheet(string mainSheetId, string sheetName)
        {
            if (_sheetService == default)
                return -1;

            var sheets = await GetSheetsInSheet(mainSheetId);
            if (sheets == default)
                return -1;

            foreach (var sheet in sheets)
            {
                if (sheetName.Equals(sheet.Properties.Title, StringComparison.Ordinal))
                    return (int)sheet.Properties.SheetId!;
            }
            LoggerType.GoogleCommunication.Log(LoggingLevel.Error, $"Could not find sheet called '{sheetName}' in '{mainSheetId}'");
            return -1;
        }

        private async Task<List<Sheet>> GetSheetsInSheet(string mainSheetId)
        {
            if (_sheetService == default)
                return new();

            var request = _sheetService.Spreadsheets.Get(mainSheetId);
            var response = await request.ExecuteAsync();

            if (response != default)
                return response.Sheets.ToList();

            LoggerType.GoogleCommunication.Log(LoggingLevel.Error, $"Could not get sheets in '{mainSheetId}'");
            return new();
        }
    }
}