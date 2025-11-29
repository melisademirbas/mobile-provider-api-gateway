using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq; // LINQ metotları için gerekli
using MobileProviderApi.Data;
using MobileProviderApi.Models;

[ApiController]
[Route("api/v1/[controller]")] 
public class BillsController : ControllerBase
{
    private readonly MobileProviderContext _context;

    public BillsController(MobileProviderContext context)
    {
        _context = context;
    }

    // [1] Query Bill (Limit 3 per day, Auth GEREKLİ)
    [Authorize] 
    [HttpGet("QueryBill/{subscriberNo}")]
    public async Task<IActionResult> QueryBill(int subscriberNo, [FromQuery] string month)
    {
        var bill = await _context.Bills
            .Where(b => b.SubscriberNo == subscriberNo && b.Month == month)
            .Select(b => new { b.BillTotal, b.PaidStatus }) 
            .FirstOrDefaultAsync();

        if (bill == null) return NotFound("Fatura bulunamadı.");
        return Ok(bill);
    }
    
    // [2] Query Bill Detailed (Auth GEREKLİ, Paging GEREKLİ)
    [Authorize]
    [HttpGet("QueryBillDetailed/{subscriberNo}")]
    public async Task<IActionResult> QueryBillDetailed(
        int subscriberNo, 
        [FromQuery] string month, 
        [FromQuery] int pageNo = 1, 
        [FromQuery] int pageSize = 10)
    {
        var totalBills = await _context.Bills.CountAsync(b => b.SubscriberNo == subscriberNo && b.Month == month);
        if (totalBills == 0) return NotFound("Detaylı fatura bulunamadı.");
        
        var billDetails = await _context.Bills
            .Where(b => b.SubscriberNo == subscriberNo && b.Month == month)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new { b.BillTotal, b.BillDetails }) 
            .ToListAsync();
        
        return Ok(new { TotalCount = totalBills, PageNumber = pageNo, PageSize = pageSize, Data = billDetails });
    }

    // [3] Banking App Query Bill (Ödenmemiş Faturalar, Auth GEREKLİ)
    [Authorize]
    [HttpGet("BankingAppQueryBill/{subscriberNo}")]
    public async Task<IActionResult> BankingAppQueryBill(int subscriberNo)
    {
        var unpaidBills = await _context.Bills
            .Where(b => b.SubscriberNo == subscriberNo && b.PaidStatus != "Paid")
            .OrderBy(b => b.Month)
            .Select(b => new 
            {
                b.Month, 
                BillToPay = b.RemainingAmount > 0 ? b.RemainingAmount : b.BillTotal
            })
            .ToListAsync();

        if (!unpaidBills.Any())
        {
            return Ok(new { Message = "Ödenmemiş fatura bulunmamaktadır.", Bills = unpaidBills });
        }
        return Ok(unpaidBills);
    }

    // [4] Pay Bill (Auth GEREKLİ DEĞİL)
    [HttpPost("PayBill")]
    public async Task<IActionResult> PayBill([FromBody] PaymentRequest request)
    {
        var bill = await _context.Bills.FirstOrDefaultAsync(b => 
            b.SubscriberNo == request.SubscriberNo && b.Month == request.Month);

        if (bill == null) return NotFound(new { PaymentStatus = "Error", TransactionStatus = "Fatura bulunamadı." });
        if (bill.PaidStatus == "Paid") return BadRequest(new { PaymentStatus = "Error", TransactionStatus = "Fatura zaten tamamen ödendi." });

        decimal amountPaid = request.Amount; 
        decimal currentRemaining = bill.RemainingAmount > 0 ? bill.RemainingAmount : bill.BillTotal;

        if (amountPaid >= currentRemaining)
        {
            bill.PaidStatus = "Paid";
            bill.RemainingAmount = 0;
        }
        else
        {
            bill.PaidStatus = "Partial";
            bill.RemainingAmount = currentRemaining - amountPaid;
        }

        try
        {
            await _context.SaveChangesAsync();
            
            return Ok(new 
            {
                PaymentStatus = bill.PaidStatus == "Paid" ? "Successful" : "Partial Payment",
                TransactionStatus = $"Ödeme işlendi. Kalan Bakiye: {bill.RemainingAmount}",
                RemainingBalance = bill.RemainingAmount
            });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { PaymentStatus = "Error", TransactionStatus = "Veritabanı güncelleme hatası." });
        }
    }

    // [5] Admin - Add Bill (Auth GEREKLİ)
    [Authorize] 
    [HttpPost("Admin/AddBill")]
    public async Task<IActionResult> AddBill([FromBody] BillAddRequest request)
    {
        var subscriberExists = await _context.Subscribers.AnyAsync(s => s.SubscriberNo == request.SubscriberNo);
        if (!subscriberExists)
        {
            return NotFound(new { TransactionStatus = $"Subscriber {request.SubscriberNo} bulunamadı. Lütfen önce abone ekleyin." });
        }

        var existingBill = await _context.Bills.FirstOrDefaultAsync(b =>
            b.SubscriberNo == request.SubscriberNo && b.Month == request.Month);

        if (existingBill != null)
        {
            return BadRequest(new { TransactionStatus = "Bu ay için fatura zaten mevcut." });
        }
        
        var newBill = new Bill
        {
            SubscriberNo = request.SubscriberNo,
            Month = request.Month,
            BillTotal = request.BillTotal,
            BillDetails = request.BillDetails,
            PaidStatus = "Unpaid",
            RemainingAmount = request.BillTotal
        };

        _context.Bills.Add(newBill);

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { TransactionStatus = "Fatura başarıyla eklendi." });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { TransactionStatus = "Hata: Veritabanına eklenirken sorun oluştu." });
        }
    }
}