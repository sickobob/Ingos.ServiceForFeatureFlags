using Ingos.ServiceForFeatureFlags.Server.Models;
using Ingos.ServiceForFeatureFlags.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ingos.ServiceForFeatureFlags.Server.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class ApiController : ControllerBase
    {
        private readonly PostgreSqlService _postgreSqlService;

        public ApiController(PostgreSqlService postgreSqlService)
        {
            _postgreSqlService = postgreSqlService;
        }

        [HttpPost("create")]
        public IActionResult Create(Setting setting)
        {
            if (сheckValuesForCreateMethod(setting))
            {
                if (!_postgreSqlService.IsExistSetting(setting.Code))
                {
                    var userName = User.Identity?.Name;
                    setting.Isn_Name = userName;
                    _postgreSqlService.InsertSetting(setting);
                    return Ok(setting);
                }
            }

            return BadRequest();
        }

        [HttpGet("read")]
        public IActionResult Read(string code, string settingType)
        {
            if (_postgreSqlService.IsExistSetting(code))
            {
                var setting = _postgreSqlService.GetSetting(code, settingType);
                return Ok(setting);
            }

            return BadRequest();
        }

        [HttpGet("readAllSettings")]
        public IEnumerable<Setting> GetAllSettings()
        {
            return _postgreSqlService.RetrieveAllSettings();
        }

        [HttpPut("update")]
        public IActionResult Update(List<Setting> settings)
        {
            var userName = User.Identity?.Name;
            foreach (var setting in settings)
            {
                setting.Isn_Name = userName!;
                if (!_postgreSqlService.UpdateSetting(setting))
                    return BadRequest($"setting с кодом {setting.Code} не удалось обновить");
            }

            return Ok($"Обновлено {settings.Count}");
        }

        [HttpDelete("delete")]
        public IActionResult Delete(string code)
        {
            if (_postgreSqlService.DeleteSetting(code))
                return Ok($"setting {code} удалена");
            return BadRequest();
        }

        [HttpPost("connect")]
        public IActionResult Connect([FromBody] string dbName)
        {
            _postgreSqlService.SetConnectionString(dbName);

            bool isConnected = _postgreSqlService.ConnectToDataBase();
            if (isConnected)
            {
                return Ok("Подключение успешно установлено.");
            }

            return StatusCode(500, $"Подключение не установлено");
        }

        [HttpGet("getDatabases")]
        public IActionResult GetDatabases()
        {
            List<string> dbNames = _postgreSqlService.GetAllDataBasesNames();
            return Ok(dbNames);
        }

        bool сheckValuesForCreateMethod(Setting setting)
        {
            if (
                string.IsNullOrEmpty(setting.Type) ||
                string.IsNullOrEmpty(setting.Name) ||
                string.IsNullOrEmpty(setting.Code) ||
                string.IsNullOrEmpty(setting.Status.ToString()))
            {
                return false;
            }

            return true;
        }
    }
}