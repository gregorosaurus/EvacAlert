@description('The environment that the resources are being deployed for')
@allowed([
    'dev'
    'test'
    'prod'
])
param environment string = 'dev'

param location string = 'canadacentral'
param locationShortCode string = 'cc'

param companyident string = ''

param azmapslocation string = 'westus2'

@description('Used for hosting data for the evac alert solution.  Both upload and output data. ')
resource evacAlertADLSAccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: 'adls${companyident}evacalert${environment}${locationShortCode}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true //could be set to false
    isHnsEnabled: true
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
  }
}

@description('Used for hosting the function app data')
resource evacAlertStorageAccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: 'st${companyident}evacalert${environment}${locationShortCode}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    isHnsEnabled: false //function app needs a regular storage account
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
  }
}

@description('The consumption based app service plan for the azure function')
resource evacAlertFunctionAppHostingPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'asp-${companyident}-evacalert-${environment}-${locationShortCode}'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
    capacity: 0
  }
}


@description('The main function app hosting API calls for ADF')
resource evacAlertFunctionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: 'func-${companyident}-evacalert-${environment}-${locationShortCode}'
  location: location
  kind: 'functionapp'
  identity:{
    type:'SystemAssigned'    
  }
  properties: {
    serverFarmId: evacAlertFunctionAppHostingPlan.id
    siteConfig: {
      netFrameworkVersion:'v6.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${evacAlertStorageAccount.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${evacAlertStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${evacAlertStorageAccount.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${evacAlertStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'func-${companyident}-evacalert-${environment}-${locationShortCode}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
            name: 'AzureMapsApiKey'
            value: 'MANUALLY_ENTER_POST_DEPLOYMENT'
        }
      ]
    }
  }
}


@description('The main orchestration engine, Azure Data Factory, for evac alert')
resource evacAlertDataFactory 'Microsoft.DataFactory/factories@2018-06-01' = {
  name: 'adf-${companyident}-evacalert-${environment}-${locationShortCode}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
}

resource evacAlertADLSAccountADFLinkedService 'Microsoft.DataFactory/factories/linkedservices@2018-06-01' = {
  parent: evacAlertDataFactory
  name: 'EvacAlertADLSLinkedService'
  properties: {
    type: 'AzureBlobFS'
    typeProperties: {
      accountKey: evacAlertADLSAccount.listKeys().keys[0].value
      url: 'https://${evacAlertADLSAccount.name}.dfs.core.windows.net'
    }
  }
}

resource evacAlertFunctionAppADFLinkedService 'Microsoft.DataFactory/factories/linkedservices@2018-06-01' = {
  parent:evacAlertDataFactory
  name: 'EvacAlertFunctionApp'
  properties: {
    type: 'HttpServer'
    typeProperties: {
      url: 'https://${evacAlertFunctionApp.properties.defaultHostName}/api'
      enableServerCertificateValidation: true
      authenticationType: 'Anonymous'
  }
  }
}

resource addressesADFDataSet 'Microsoft.DataFactory/factories/datasets@2018-06-01' = {
  parent: evacAlertDataFactory
  name: 'EvacAlertAddresses'
  properties: {
    linkedServiceName: {
      referenceName: evacAlertADLSAccountADFLinkedService.name
      type: 'LinkedServiceReference'
    }
    type: 'DelimitedText'
    typeProperties: {
      location: {
        type:'AzureBlobFSLocation'
        fileName: 'addresses.csv'
        folderPath: 'upload'
        fileSystem: 'data'
      }
      columnDelimiter:','
      quoteChar:'"'
      escapeChar:'\\'
      firstRowAsHeader: true
    }
    schema: [
      {
        name:'identifier'
        type: 'String'
      }
      {
          name: 'address'
          type: 'String'
      }
    ]
  }
}

resource httpGeocodedAddressesDataSet 'Microsoft.DataFactory/factories/datasets@2018-06-01' = {
  parent: evacAlertDataFactory
  name: 'EvacAlertFunctionGeoCodedResponse'
  properties: {
    linkedServiceName: {
      referenceName: evacAlertFunctionAppADFLinkedService.name
      type: 'LinkedServiceReference'
    }
    type: 'Json'
    typeProperties: {
      location: {
        type:'HttpServerLocation'
        relativeUrl:'GeocodeAddresses'
      }
    }
    schema: {
      type: 'object'
      properties: {
        identifier: {
          type:'string'
        }
        coordinate: {
          type:'object'
          properties: {
            latitude: {
              type:'number'
            }
            longitude: {
              type: 'number'
            }
          }
        }
      }
    }
  }
}



resource geocodedDataADFDataSet 'Microsoft.DataFactory/factories/datasets@2018-06-01' = {
  parent: evacAlertDataFactory
  name: 'EvacAlertGeoCodedData'
  properties: {
    linkedServiceName: {
      referenceName: evacAlertADLSAccountADFLinkedService.name
      type: 'LinkedServiceReference'
    }
    type: 'Json'
    typeProperties: {
      location: {
        type:'AzureBlobFSLocation'
        fileName: 'geocoded.json'
        folderPath: 'generated'
        fileSystem: 'data'
      }
    }
    schema: {
      type: 'object'
      properties: {
        identifier: {
          type:'string'
        }
        coordinate: {
          type:'object'
          properties: {
            latitude: {
              type:'number'
            }
            longitude: {
              type: 'number'
            }
          }
        }
      }
    }
  }
}



resource mapAccount 'Microsoft.Maps/accounts@2021-02-01' = {
    name: 'azmaps-${companyident}-evacalert-${environment}-${locationShortCode}'
    location: azmapslocation  //not all regions are supported
    sku: {
        name: 'G2'
    }
    kind: 'Gen2'
}
