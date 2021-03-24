﻿using Inotify.Data;
using Inotify.Data.Models;
using Inotify.Sends;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Inotify.Controllers
{
    [ApiController]
    [Route("api/setting")]
    public class SettingControlor : BaseController
    {
        [HttpGet, Authorize(Policys.SystemOrUsers)]
        public JsonResult Index()
        {
            return OK();
        }

        [HttpGet, Route("GetSendTemplates"), Authorize(Policys.SystemOrUsers)]
        public JsonResult GetSendTemplates()
        {
            var sendTemplates = SendTaskManager.Instance.GetInputTemeplates().Values;
            return OK(sendTemplates);
        }

        [HttpGet, Route("GetSendAuths"), Authorize(Policys.SystemOrUsers)]
        public JsonResult GetSendAuths()
        {
            var userInfo = DBManager.Instance.GetUser(UserName);
            if (userInfo != null)
            {
                var sendAuthInfos = DBManager.Instance.DBase.Query<SendAuthInfo>().Where(e => e.UserId == userInfo.Id).ToArray();
                var userSendTemplates = new List<InputTemeplate>();
                foreach (var sendAuthInfo in sendAuthInfos)
                {
                    var sendTemplate = SendTaskManager.Instance.GetInputTemplate(sendAuthInfo.SendMethodTemplate);
                    if (sendTemplate != null)
                    {
                        sendTemplate.Name = sendAuthInfo.Name;
                        sendTemplate.AuthData = sendAuthInfo.AuthData;
                        sendTemplate.SendAuthId = sendAuthInfo.Id;
                        sendTemplate.IsActive = sendAuthInfo.Id == userInfo.SendAuthId;
                        sendTemplate.AuthToTemplate(sendAuthInfo.AuthData);
                        userSendTemplates.Add(sendTemplate);
                    }
                }

                return OK(userSendTemplates);
            }
            return Fail();
        }

        [HttpPost, Route("ActiveSendAuth"), Authorize(Policys.SystemOrUsers)]
        public JsonResult ActiveSendAuth(int sendAuthId, bool state)
        {
            var userInfo = DBManager.Instance.GetUser(UserName);
            if (userInfo != null)
            {
                var authInfo = DBManager.Instance.DBase.Query<SendAuthInfo>().FirstOrDefault(e => e.Id == sendAuthId && e.UserId == userInfo.Id);
                if (authInfo != null)
                {
                    userInfo.SendAuthId = state ? sendAuthId : -1;
                    DBManager.Instance.DBase.Update(userInfo);
                    return OK(userInfo);
                }
            }
            return Fail();
        }

        [HttpPost, Route("DeleteSendAuth"), Authorize(Policys.SystemOrUsers)]
        public JsonResult DeleteSendAuth(int sendAuthId)
        {
            var userInfo = DBManager.Instance.GetUser(UserName);
            if (userInfo != null)
            {
                var authInfo = DBManager.Instance.DBase.Query<SendAuthInfo>().FirstOrDefault(e => e.Id == sendAuthId && e.UserId == userInfo.Id);
                if (authInfo != null)
                {
                    DBManager.Instance.DBase.Delete(authInfo);
                    return OK();
                }
            }
            return Fail();
        }

        [HttpPost, Route("AddSendAuth"), Authorize(Policys.SystemOrUsers)]
        public JsonResult AddSendAuth(InputTemeplate inputTemeplate)
        {
            var userInfo = DBManager.Instance.GetUser(UserName);
            if (userInfo != null && inputTemeplate.Key != null && inputTemeplate.Name != null)
            {
                var authInfo = inputTemeplate.TemplateToAuth();
                var sendAuth = new SendAuthInfo()
                {
                    UserId = userInfo.Id,
                    SendMethodTemplate = inputTemeplate.Key,
                    AuthData = authInfo,
                    Name = inputTemeplate.Name,
                    CreateTime = DateTime.Now,
                    ModifyTime = DateTime.Now,
                };
                DBManager.Instance.DBase.Insert(sendAuth);
                return OK(sendAuth);
            }
            return Fail();
        }

        [HttpPost, Route("ModifySendAuth"), Authorize(Policys.SystemOrUsers)]
        public JsonResult ModifySendAuth(InputTemeplate inputTemeplate)
        {
            var userInfo = DBManager.Instance.GetUser(UserName);
            if (userInfo != null)
            {
                var oldSendInfo = DBManager.Instance.DBase.Query<SendAuthInfo>().FirstOrDefault(e => e.Id == inputTemeplate.SendAuthId);
                if (oldSendInfo != null && inputTemeplate.Name != null)
                {
                    oldSendInfo.Name = inputTemeplate.Name;
                    oldSendInfo.AuthData = inputTemeplate.TemplateToAuth();
                    oldSendInfo.ModifyTime = DateTime.Now;
                    DBManager.Instance.DBase.Update(oldSendInfo);
                }
                return OK(oldSendInfo);
            }
            return Fail();
        }

        [HttpGet, Route("GetSendKey"), Authorize(Policys.SystemOrUsers)]
        public JsonResult GetSendKey()
        {
            var userInfo = DBManager.Instance.GetUser(UserName);
            if (userInfo != null)
                return OK(userInfo.Token);
            return Fail();

        }

        [HttpGet, Route("ReSendKey"), Authorize(Policys.SystemOrUsers)]
        public JsonResult ReSendKey()
        {
            var userInfo = DBManager.Instance.GetUser(UserName);
            if (userInfo != null)
            {
                userInfo.Token = Guid.NewGuid().ToString("N").ToUpper();
                DBManager.Instance.DBase.Update(userInfo);
                return OK(userInfo.Token);
            }
            return Fail();
        }
    }
}