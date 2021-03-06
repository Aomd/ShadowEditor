﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShadowEditor.Model.Material;
using ShadowEditor.Server.Base;
using ShadowEditor.Server.Helpers;
using ShadowEditor.Server.CustomAttribute;

namespace ShadowEditor.Server.Controllers
{
    /// <summary>
    /// 材质控制器
    /// </summary>
    public class MaterialController : ApiBase
    {
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authority("LIST_MATERIAL")]
        public JsonResult List()
        {
            var mongo = new MongoHelper();

            // 获取所有类别
            var filter = Builders<BsonDocument>.Filter.Eq("Type", "Material");
            var categories = mongo.FindMany(Constant.CategoryCollectionName, filter).ToList();

            var docs = new List<BsonDocument>();

            if (ConfigHelper.EnableAuthority)
            {
                var user = UserHelper.GetCurrentUser();

                if (user != null)
                {
                    var filter1 = Builders<BsonDocument>.Filter.Eq("UserID", user.ID);

                    if (user.Name == "Administrator")
                    {
                        var filter2 = Builders<BsonDocument>.Filter.Exists("UserID");
                        var filter3 = Builders<BsonDocument>.Filter.Not(filter2);
                        filter1 = Builders<BsonDocument>.Filter.Or(filter1, filter3);
                    }
                    docs = mongo.FindMany(Constant.MaterialCollectionName, filter1).ToList();
                }
            }
            else
            {
                docs = mongo.FindAll(Constant.MaterialCollectionName).ToList();
            }

            var list = new List<MaterialModel>();

            foreach (var i in docs)
            {
                var categoryID = "";
                var categoryName = "";

                if (i.Contains("Category") && !i["Category"].IsBsonNull && !string.IsNullOrEmpty(i["Category"].ToString()))
                {
                    var doc = categories.Where(n => n["_id"].ToString() == i["Category"].ToString()).FirstOrDefault();
                    if (doc != null)
                    {
                        categoryID = doc["_id"].ToString();
                        categoryName = doc["Name"].ToString();
                    }
                }

                var info = new MaterialModel
                {
                    ID = i["ID"].AsObjectId.ToString(),
                    Name = i["Name"].AsString,
                    CategoryID = categoryID,
                    CategoryName = categoryName,
                    TotalPinYin = i["TotalPinYin"].ToString(),
                    FirstPinYin = i["FirstPinYin"].ToString(),
                    CreateTime = i["CreateTime"].ToUniversalTime(),
                    UpdateTime = i["UpdateTime"].ToUniversalTime(),
                    Thumbnail = i.Contains("Thumbnail") && !i["Thumbnail"].IsBsonNull ? i["Thumbnail"].ToString() : null
                };

                list.Add(info);
            }

            list = list.OrderByDescending(n => n.UpdateTime).ToList();

            return Json(new
            {
                Code = 200,
                Msg = "Get Successfully!",
                Data = list
            });
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult Get(string ID)
        {
            var mongo = new MongoHelper();

            var filter = Builders<BsonDocument>.Filter.Eq("ID", BsonObjectId.Create(ID));
            var doc = mongo.FindOne(Constant.MaterialCollectionName, filter);

            if (doc == null)
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "The material is not existed!"
                });
            }

            var obj = new JObject
            {
                ["ID"] = doc["ID"].AsObjectId.ToString(),
                ["Name"] = doc["Name"].AsString,
                ["TotalPinYin"] = doc["TotalPinYin"].ToString(),
                ["FirstPinYin"] = doc["FirstPinYin"].ToString(),
                ["CreateTime"] = doc["CreateTime"].ToUniversalTime(),
                ["UpdateTime"] = doc["UpdateTime"].ToUniversalTime(),
                ["Data"] = JsonConvert.DeserializeObject<JObject>(doc["Data"].ToJson()),
                ["Thumbnail"] = doc["Thumbnail"].ToString(),
            };

            return Json(new
            {
                Code = 200,
                Msg = "Get Successfully!",
                Data = obj
            });
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authority("EDIT_MATERIAL")]
        public JsonResult Edit(MaterialEditModel model)
        {
            var objectId = ObjectId.GenerateNewId();

            if (!string.IsNullOrEmpty(model.ID) && !ObjectId.TryParse(model.ID, out objectId))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "ID is not allowed."
                });
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "Name is not allowed to be empty."
                });
            }

            var mongo = new MongoHelper();

            var pinyin = PinYinHelper.GetTotalPinYin(model.Name);

            var filter = Builders<BsonDocument>.Filter.Eq("ID", objectId);
            var update1 = Builders<BsonDocument>.Update.Set("Name", model.Name);
            var update2 = Builders<BsonDocument>.Update.Set("TotalPinYin", pinyin.TotalPinYin);
            var update3 = Builders<BsonDocument>.Update.Set("FirstPinYin", pinyin.FirstPinYin);
            var update4 = Builders<BsonDocument>.Update.Set("Thumbnail", model.Thumbnail);

            UpdateDefinition<BsonDocument> update5;

            if (string.IsNullOrEmpty(model.Category))
            {
                update5 = Builders<BsonDocument>.Update.Unset("Category");
            }
            else
            {
                update5 = Builders<BsonDocument>.Update.Set("Category", model.Category);
            }

            var update = Builders<BsonDocument>.Update.Combine(update1, update2, update3, update4, update5);
            mongo.UpdateOne(Constant.MaterialCollectionName, filter, update);

            return Json(new
            {
                Code = 200,
                Msg = "Saved successfully!"
            });
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authority("SAVE_MATERIAL")]
        public JsonResult Save(MaterialSaveModel model)
        {
            var objectId = ObjectId.GenerateNewId();

            if (!string.IsNullOrEmpty(model.ID) && !ObjectId.TryParse(model.ID, out objectId))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "ID is not allowed."
                });
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "Name is not allowed to be empty."
                });
            }

            // 查询
            var mongo = new MongoHelper();
            var filter = Builders<BsonDocument>.Filter.Eq("ID", objectId);
            var doc = mongo.FindOne(Constant.MaterialCollectionName, filter);

            var now = DateTime.Now;

            if (doc == null) // 新建
            {
                var pinyin = PinYinHelper.GetTotalPinYin(model.Name);

                doc = new BsonDocument
                {
                    ["ID"] = objectId,
                    ["Name"] = model.Name,
                    ["CategoryID"] = 0,
                    ["CategoryName"] = "",
                    ["TotalPinYin"] = string.Join("", pinyin.TotalPinYin),
                    ["FirstPinYin"] = string.Join("", pinyin.FirstPinYin),
                    ["Version"] = 0,
                    ["CreateTime"] = BsonDateTime.Create(now),
                    ["UpdateTime"] = BsonDateTime.Create(now),
                    ["Data"] = BsonDocument.Parse(model.Data),
                    ["Thumbnail"] = model.Thumbnail
                };

                if (ConfigHelper.EnableAuthority)
                {
                    var user = UserHelper.GetCurrentUser();

                    if (user != null)
                    {
                        doc["UserID"] = user.ID;
                    }
                }

                mongo.InsertOne(Constant.MaterialCollectionName, doc);
            }
            else // 更新
            {
                var update1 = Builders<BsonDocument>.Update.Set("UpdateTime", BsonDateTime.Create(now));
                var update2 = Builders<BsonDocument>.Update.Set("Data", BsonDocument.Parse(model.Data));
                var update = Builders<BsonDocument>.Update.Combine(update1, update2);
                mongo.UpdateOne(Constant.MaterialCollectionName, filter, update);
            }

            return Json(new
            {
                Code = 200,
                Msg = "Saved successfully!",
                ID = objectId
            });
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [HttpPost]
        [Authority("DELETE_MATERIAL")]
        public JsonResult Delete(string ID)
        {
            var mongo = new MongoHelper();

            var filter = Builders<BsonDocument>.Filter.Eq("ID", BsonObjectId.Create(ID));
            var doc = mongo.FindOne(Constant.MaterialCollectionName, filter);

            if (doc == null)
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "The asset is not existed!"
                });
            }

            mongo.DeleteOne(Constant.MaterialCollectionName, filter);

            return Json(new
            {
                Code = 200,
                Msg = "Delete successfully!"
            });
        }
    }
}
