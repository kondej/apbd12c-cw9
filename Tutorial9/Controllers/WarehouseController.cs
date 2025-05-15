using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IDbService _dbService;

        public WarehouseController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] WarehouseDTO dto)
        {
            try
            {
                var id = await _dbService.AddProductToWarehouseAsync(dto);

                if (id == -1)
                {
                    return NotFound("Nie znaleziono produktu!");
                } else if (id == -2)
                {
                    return NotFound("Nie znaleziono magazynu!");
                } else if (id == -3)
                {
                    return BadRequest("Zamówienie zostało zrealizowane!");
                } else if (id == -4)
                {
                    return BadRequest("Nie poprawne zamówienie!");
                } else if (id == -5)
                {
                    return Conflict("Takie zamówienie już istnieje!");
                }
                
                return CreatedAtAction(nameof(AddProductToWarehouse), new { id = id }, id);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("procedure")]
        public async Task<IActionResult> AddProductToWarehouseProcedure([FromBody] WarehouseDTO dto)
        {
            try
            {
                var id = await _dbService.AddProductToWarehouseProcedureAsync(dto);
                
                return CreatedAtAction(nameof(AddProductToWarehouseProcedure), new { id = id }, id);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
