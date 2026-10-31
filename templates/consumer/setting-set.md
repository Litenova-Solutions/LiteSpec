---
{
  "type": "setting-set",
  "title": "<Settings Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "records": [
    {
      "type": "setting",
      "id": "setting/<module>.<name>",
      "title": "<Setting Display Title>",
      "valueType": "<integer|duration|money|boolean|text|choice>",
      "default": "<value>",
      "range": "<min..max or choices>",
      "unit": "<unit>",
      "setBy": "<decider|operator|tenant>",
      "atBound": "<refuse|truncate|degrade>",
      "secret": false,
      "fulfills": ["requirement/<topic>.<name>"]
    }
  ]
}
---
# <Settings Display Title>
