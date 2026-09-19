param amount int
param notificationEmail string
param startDate string

resource monthlyBudget 'Microsoft.Consumption/budgets@2023-05-01' = {
  name: 'cabrynt-monthly-budget'
  properties: {
    category: 'Cost'
    amount: amount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: '${startDate}T00:00:00Z'
      endDate: '2027-12-31T00:00:00Z'
    }
    notifications: {
      actualCost50: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 50
        thresholdType: 'Percentage'
        contactEmails: [
          notificationEmail
        ]
      }
      actualCost75: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 75
        thresholdType: 'Percentage'
        contactEmails: [
          notificationEmail
        ]
      }
      forecastCost90: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 90
        thresholdType: 'Percentage'
        contactEmails: [
          notificationEmail
        ]
      }
    }
  }
}
