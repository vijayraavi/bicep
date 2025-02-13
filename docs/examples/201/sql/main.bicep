param sqlAdministratorLogin string
param sqlAdministratorLoginPassword string {
  secure: true
}

param transparentDataEncryption string {
  default: 'Enabled'
  allowed: [
    'Enabled'
    'Disabled'
  ]
}

param location string = resourceGroup().location

var databaseName = 'sample-db-with-tde'
var databaseEdition = 'Basic'
var databaseCollation = 'SQL_Latin1_General_CP1_CI_AS'
var databaseServiceObjectiveName = 'Basic'

var sqlServerName = 'sqlserver${uniqueString(resourceGroup().id)}'

resource sqlServer 'Microsoft.Sql/servers@2019-06-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
    version: '12.0'
  }
}

resource db 'Microsoft.Sql/servers/databases@2019-06-01-preview' = {
  name: '${sqlServer.name}/${databaseName}'
  location: location
  sku: {
    name: databaseServiceObjectiveName
    tier: databaseEdition
  }
  properties: {
    collation: databaseCollation
  }
}

// very long type...
resource tde 'Microsoft.Sql/servers/databases/transparentDataEncryption@2014-04-01' = {
  name: '${db.name}/current'
  properties: {
    status: transparentDataEncryption
  }
}