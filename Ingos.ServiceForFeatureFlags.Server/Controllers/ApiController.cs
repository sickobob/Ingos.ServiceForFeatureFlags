using Ingos.ServiceForFeatureFlags.Server.Models;
using Ingos.ServiceForFeatureFlags.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ingos.ServiceForFeatureFlags.Server.Controllers
{
    [Route("api")]
    [ApiController]
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
                    _postgreSqlService.InsertSetting(setting);
                    return Ok(setting);
                }
            }

            return BadRequest();
        }
        [HttpGet("read")]
        public IActionResult Read(string code,string setting_type)
        {
            if (_postgreSqlService.IsExistSetting(code))
            {
                var setting = _postgreSqlService.GetSetting(code, setting_type);
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
        public IActionResult Update(string code, bool status, DateTime datetime=default, string stringvalue="Undefined", int intvalue=0)
        {
            if(_postgreSqlService.UpdateSetting(code, status, datetime, stringvalue, intvalue)) return Ok();
            return BadRequest();
        }
        
        [HttpDelete("delete")]
        public IActionResult Delete(string code)
        {
            if(_postgreSqlService.DeleteSetting(code))
                return Ok(new {message = $"setting {code} удалена"});
            return BadRequest();
        }
        
        private bool сheckValuesForCreateMethod(Setting setting)
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