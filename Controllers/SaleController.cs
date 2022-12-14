using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CashRegister.Models;

namespace CashRegister.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaleController : ControllerBase
    {
        private readonly CashRegisterContext _context;

        public SaleController(CashRegisterContext context)
        {
            _context = context;
        }

        // GET: api/Sale
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSales()
        {
          if (_context.Sales == null)
          {
              return NotFound();
          }
            return await _context.Sales.Include("ProductSales.Product").ToListAsync();
        }

        // GET: api/Sale/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetSale(int id)
        {
          if (_context.Sales == null)
          {
              return NotFound();
          }
            var sale = await _context.Sales.Include(s => s.ProductSales).FirstOrDefaultAsync(s => s.SaleId == id);

            if (sale == null)
            {
                return NotFound();
            }

            return sale;
        }

        // PUT: api/Sale/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSale(int id, Sale sale)
        {
            if (id != sale.SaleId)
            {
                return BadRequest();
            }

            _context.Entry(sale).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SaleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Sale
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Sale>> PostSale(SaleRequest saleRequest)
        {
          if (_context.Sales == null)
          {
              return Problem("Entity set 'CashRegisterContext.Sales'  is null.");
          }

          var sale = new Sale();

          sale.ApartmentNumber = saleRequest.ApartmentNumber;
          sale.IsLoan = saleRequest.IsLoan;
          sale.Payment = saleRequest.Payment;
          var productIds = saleRequest.ProductSales.Select(ps => ps.ProductId).ToList();

          var products = await _context.Products .Where(p => productIds.Contains(p.ProductId))
                                                .ToListAsync();

        var productSales = new List<ProductSale>();
        var total = 0;
        foreach (var productSaleRequest in saleRequest.ProductSales)

        {
            var product = products.Find(p => p.ProductId == productSaleRequest.ProductId && p.IsActive);
            if (product == null)
            {
                return BadRequest(new 
                {
                Error = "Invalid Product"
                });
            }
            if (product.Quantity < productSaleRequest.Quantity)
            {
                    return BadRequest(new{
                        Error = $"Insuficcient inventory for product {product.Name}"
                    });
            }

            product.Quantity -= productSaleRequest.Quantity;
            _context.Entry(sale).State = EntityState.Modified;

            total += product.SalePrice * productSaleRequest.Quantity;
            productSales.Add(new ProductSale {
                Price = product.SalePrice,
                ProductId = productSaleRequest.ProductId,
                Quantity = productSaleRequest.Quantity
            });

        
        }                                               
          sale.ProductSales = productSales;
        
        if (sale.Payment < total && !sale.IsLoan)
        {
            return BadRequest(new {
            Error = "The total value is higher than the Payment"
            });
        }
        
          sale.Total = total;
          sale.Date = DateTime.Now;
          if (sale.IsLoan)
          {
            sale.Payment = 0;
            if (string.IsNullOrEmpty(sale.ApartmentNumber))
            {
                return BadRequest(new {
            Error = "the apartment number is required if it a loan"
            });
            }
          }
          else if (sale.Payment < sale.Total)
           { return BadRequest(new {
            Error = "the payment is insufficient"
            });
          }

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSale), new { id = sale.SaleId }, sale);
        }

        // DELETE: api/Sale/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSale(int id)
        {
            if (_context.Sales == null)
            {
                return NotFound();
            }
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
            {
                return NotFound();
            }

            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SaleExists(int id)
        {
            return (_context.Sales?.Any(e => e.SaleId == id)).GetValueOrDefault();
        }
    }
}
