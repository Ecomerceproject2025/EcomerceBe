using Microsoft.AspNetCore.Mvc;
using EcomerceBE.DTOs.Contact;
using EcomerceBE.Service;
using EcomerceBE.Models;
using EcomerceBE.Data;
using Microsoft.EntityFrameworkCore;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ContactController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("send")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> SendContactMessage([FromBody] ContactRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Find user by email (optional - if user exists, link to their account)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                int? userId = user?.Id;

                // Save contact message to database
                var contact = new Contact
                {
                    UserId = userId ?? 0, // Use 0 if user not found (guest contact)
                    Subject = $"Contact from {request.Name}",
                    Message = $"Name: {request.Name}\nEmail: {request.Email}\nPhone: {request.Phone}\n\nMessage:\n{request.Message}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                // Send email notification to admin
                var emailService = new Email(_configuration);
                var adminEmail = _configuration["EmailSettings:AdminEmail"] ?? _configuration["EmailSettings:From"];
                
                var emailSubject = $"New Contact Message from {request.Name}";
                var emailBody = $@"
                    <h2>New Contact Message</h2>
                    <p><strong>Name:</strong> {request.Name}</p>
                    <p><strong>Email:</strong> {request.Email}</p>
                    <p><strong>Phone:</strong> {request.Phone}</p>
                    <p><strong>Message:</strong></p>
                    <p>{request.Message.Replace("\n", "<br>")}</p>
                    <hr>
                    <p><small>Received at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</small></p>
                ";

                try
                {
                    await emailService.SendEmailAsync(adminEmail!, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    // Log email error but don't fail the request
                    Console.WriteLine($"[ERROR] Failed to send contact email: {ex.Message}");
                }

                // Send confirmation email to user
                var confirmationSubject = "Thank you for contacting us";
                var confirmationBody = $@"
                    <h2>Thank you for contacting us!</h2>
                    <p>Dear {request.Name},</p>
                    <p>We have received your message and will get back to you within 24 hours.</p>
                    <p>Your message:</p>
                    <p style=""background-color: #f5f5f5; padding: 15px; border-radius: 5px;"">{request.Message.Replace("\n", "<br>")}</p>
                    <p>Best regards,<br>E-Commerce Team</p>
                ";

                try
                {
                    await emailService.SendEmailAsync(request.Email, confirmationSubject, confirmationBody);
                }
                catch (Exception ex)
                {
                    // Log email error but don't fail the request
                    Console.WriteLine($"[ERROR] Failed to send confirmation email: {ex.Message}");
                }

                return Ok(new { message = "Your message has been sent successfully. We will contact you soon." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ContactController.SendContactMessage failed: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while sending your message. Please try again later." });
            }
        }

        [HttpGet("list")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetContactMessages([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            try
            {
                var skip = (page - 1) * limit;
                var total = await _context.Contacts.CountAsync();
                
                var contacts = await _context.Contacts
                    .Include(c => c.User)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.ContactId,
                        c.UserId,
                        UserName = c.User != null ? c.User.Name : "Guest",
                        UserEmail = c.User != null ? c.User.Email : null,
                        c.Subject,
                        c.Message,
                        c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total,
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling(total / (double)limit),
                    data = contacts
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ContactController.GetContactMessages failed: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while fetching contact messages." });
            }
        }
    }
}

