using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalamatyAPI.Data; // تأكدي أن هذا المسار يشير لمجلد الداتا الذي يحتوي على ApplicationDbContext
using SalamatyAPI.Dtos.Favorites;
using SalamatyAPI.Models;

namespace SalamatyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        // تم تغيير النوع هنا من SalamatyDbContext إلى ApplicationDbContext
        private readonly ApplicationDbContext _db;

        public FavoritesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /api/favorites/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<FavoriteDto>>> GetFavoritesForUser(string userId)
        {
            var favorites = await _db.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .Select(f => new FavoriteDto
                {
                    FavoriteId = f.FavoriteId,
                    Product = new ProductInFavoriteDto
                    {
                        Id = f.Product.Id,
                        Name = f.Product.Name,
                        ImageUrl = f.Product.ImageUrl,
                        Description = f.Product.Description
                    }
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // POST /api/favorites
        [HttpPost]
        public async Task<ActionResult<FavoriteDto>> AddFavorite([FromBody] CreateFavoriteDto request)
        {
            var product = await _db.Products.FindAsync(request.ProductId);
            if (product == null) return BadRequest("Invalid productId");

            var favorite = new Favorite
            {
                UserId = request.UserId, // كلاهما الآن string GUID
                ProductId = request.ProductId
            };

            _db.Favorites.Add(favorite);
            await _db.SaveChangesAsync();

            var dto = new FavoriteDto
            {
                FavoriteId = favorite.FavoriteId,
                Product = new ProductInFavoriteDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl,
                    Description = product.Description
                }
            };

            return CreatedAtAction(
                nameof(GetFavoritesForUser),
                new { userId = request.UserId },
                dto);
        }

        // DELETE /api/favorites/{favoriteId}
        [HttpDelete("{favoriteId:int}")]
        public async Task<IActionResult> DeleteFavorite(int favoriteId)
        {
            var favorite = await _db.Favorites.FindAsync(favoriteId);
            if (favorite == null) return NotFound();

            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}