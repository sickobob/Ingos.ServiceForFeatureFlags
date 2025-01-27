using Ingos.ServiceForFeatureFlags.Server.Helpers;
using Ingos.ServiceForFeatureFlags.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ingos.ServiceForFeatureFlags.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly PostgreSqlService _postgreSqlService;

        public ApiController(PostgreSqlService postgreSqlService)
        {
            _postgreSqlService = postgreSqlService;
        }

        [HttpPost("Create")]
        public IActionResult Create(Setting setting)
        {
            if (CheckValuesForCreateMethod(setting))
            {
                if (!_postgreSqlService.IsExistSetting(setting.Code))
                {
                    _postgreSqlService.InsertSetting(setting);
                    return Ok(new { message = "setting создана" });
                }
            }

            return BadRequest();
        }
        [HttpGet("Read")]
        public Setting Read(string code,string setting_type)
        {
            if (_postgreSqlService.IsExistSetting(code))
            {
                var setting = _postgreSqlService.GetSetting(code, setting_type);
                return setting;
            }

            return null;
        }

        [HttpPut("Update")]
        public IActionResult Update(string code, bool status, DateTime datetime=default, string stringvalue="Undefined", int intvalue=0)
        {
            if(_postgreSqlService.UpdateSetting(code, status, datetime, stringvalue, intvalue)) return Ok();
            return BadRequest();
        }
        
        [HttpPut("Delete")]
        public IActionResult Delete(string code)
        {
            if(_postgreSqlService.DeleteSetting(code))
                return Ok(new {message = "setting удалена"});
            return BadRequest();
        }
        
        private bool CheckValuesForCreateMethod(Setting setting)
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