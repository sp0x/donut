﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Donut.Integration;
using Donut.Source;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;

namespace Donut.Encoding
{
    public class BinaryCategoryEncoding : FieldEncoding
    {

        private FieldExtra EmptyFieldExtra;
        public BinaryCategoryEncoding(FieldEncodingOptions options) : base(options, FieldDataEncoding.BinaryIntId)
        {
            Limit = 20;
            EmptyFieldExtra= new FieldExtra
            {
                Key = EncodeKey(9),
                Value = "none"
            };
        }


        public override void Apply(BsonDocument doc)
        {
            foreach (var field in TargetFields)
            {
                var docFieldVal = doc[field.Name].ToString();
                var categories = FieldCache[field.Name];
                IFieldExtra extrasCategory = null;
                if (categories.Count >= Limit)
                {
                    extrasCategory = EmptyFieldExtra;
                }
                else
                {
                    extrasCategory = categories.GetOrAdd(docFieldVal, (key) =>
                    {
                        var newExtra = new FieldExtra()
                        {
                            Field = field,
                            Key = EncodeKey(categories.Count + 1),
                            Value = docFieldVal
                        };
                        if (field.Extras == null)
                        {
                            field.Extras = new FieldExtras();
                        }
                        field.Extras.Extra.Add(newExtra);
                        return newExtra;
                    });
                }
                
                //doc[field.Name] = uint.Parse(extrasCategory.Key);
                doc[field.Name] = extrasCategory.Key;
            }
        }

        protected override string EncodeKey(int i)
        {
            var bvinary = Convert.ToString(i, 2).PadLeft(8, '0');
            bvinary = "1" + bvinary;
            return bvinary;
        }

        public override async Task<BulkWriteResult<BsonDocument>> ApplyToField(IFieldDefinition field,
            IMongoCollection<BsonDocument> collection,
            CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            var updateModels = new WriteModel<BsonDocument>[field.Extras.Extra.Count];
            int iModel = 0;
            var dummies = field.Extras.Extra;
            foreach (var column in dummies)
            {
                var query = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq(field.Name, column.Value)
                );
                var updates = new List<UpdateDefinition<BsonDocument>>();
                updates.Add(Builders<BsonDocument>.Update.Set(field.Name, column.Key));
                var qrUpdateRoot = Builders<BsonDocument>.Update.Combine(updates);
                var actionModel = new UpdateManyModel<BsonDocument>(query, qrUpdateRoot);
                updateModels[iModel++] = actionModel;
            }
            var result = await collection.BulkWriteAsync(updateModels, new BulkWriteOptions()
            {
            }, cancellationToken.Value);
            return result;
        }

        public override IIntegration GetEncodedIntegration(bool truncateDestination = false)
        {
            var collection = Integration.Collection;
            var db = MongoHelper.GetDatabase();
            if (truncateDestination)
            {
                db.DropCollection(collection);
            }
            var records = db.GetCollection<BsonDocument>(collection);
            foreach (var oneHotField in TargetFields)
            {
                var fld = oneHotField;
                //Run a group aggregate
                var pipeline = new List<BsonDocument>();
                var group = new BsonDocument();
                group["$group"] = new BsonDocument() { { "_id", $"${fld.Name}" } };
                pipeline.Add(group);
                var uniqueColumnResults = records.Aggregate<BsonDocument>(pipeline).ToList();
                int iVariation = fld.Extras.Extra == null ? 1 : fld.Extras.Extra.Count + 1;
                foreach (var uniqueValue in uniqueColumnResults)
                {
                    var columnVal = uniqueValue["_id"].ToString();
                    if (fld.Extras.Extra.Any(y => y.Key == columnVal)) continue;
                    var fieldExtra = new FieldExtra()
                    {
                        Field = fld,
                        Key = EncodeKey(iVariation++),
                        Value = columnVal,
                        Type = FieldExtraType.Dummy
                    };
                    if (fld.Extras == null) fld.Extras = new FieldExtras();
                    fld.Extras.Extra.Add(fieldExtra);
                }
            }
            return Integration;
        }

        public override IEnumerable<KeyValuePair<string, int>> GetDecodedFieldpairs(FieldDefinition field, BsonDocument document)
        {
            var fieldValue = document[field.Name].ToString();
            fieldValue = fieldValue.Substring(1);
            for (var i = 0; i < fieldValue.Length; i++)
            {
                var vfldVal = fieldValue[i].ToString();
                var vfldName = field.Name + i;
                yield return new KeyValuePair<string, int>(vfldName, int.Parse(vfldVal));
            }
        }

        public override IEnumerable<string> GetEncodedFieldNames(IFieldDefinition fld)
        {
            yield return fld.Name;
        }

        public override string DecodeField(FieldDefinition field, BsonValue value)
        {
            var fieldExtra = ExtrasRepository.GetByKey(field.Extras.Id, value.ToString());
            return fieldExtra?.Value.ToString();
        }
    }

}

