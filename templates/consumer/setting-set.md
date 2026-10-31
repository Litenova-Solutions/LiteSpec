---
{
  "type": "setting-set",
  "title": "<Settings display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "records": [
    {
      "type": "setting",
      "id": "setting/<module>.<name>",
      "title": "<Setting display title>",
      "valueType": "<integer|duration|money|boolean|text|choice>",
      "default": "<value>",
      "range": "<min..max or choices>",
      "unit": "<unit>",
      "setBy": "<decider|operator|tenant>",
      "atBound": "<refuse|truncate|degrade>",
      "secret": false,
      "fulfills": ["requirement/<topic>.<name>"],
      "readBy": ["use-case/<module>.<name>", "or remove this line when no use case reads this setting"]
    }
  ]
}
---
# <Settings Display Title>
