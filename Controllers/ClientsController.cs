using EventsService.Data;
using EventsService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EventsService.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager; // In case you need user-related info

        public ClientsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            return View(await _context.Clients.OrderBy(c => c.Name).Include(c => c.User).ToListAsync());
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.User) // Include User details
                .FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            // If you need to link to a User, you might populate a SelectList here
            // For now, keeping it simple as per plan
            return View();
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,PhoneNumber,Organization,Email,UserId")] Client client)
        {
            // Remove UserId from ModelState if it's null and not meant to be set directly,
            // or handle its binding appropriately.
            // For now, assuming UserId might be null or intentionally set.
            if (ModelState.IsValid)
            {
                _context.Add(client);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "Клиент успешно создан.";
                return RedirectToAction(nameof(Index));
            }
            // If linking to users, re-populate SelectList for UserId if necessary
            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            // If linking to users, populate SelectList for UserId
            // Example: ViewBag.UserId = new SelectList(await _userManager.Users.OrderBy(u=>u.UserName).ToListAsync(), "Id", "UserName", client.UserId);
            return View(client);
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,PhoneNumber,Organization,Email,UserId")] Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                    // TempData["SuccessMessage"] = "Данные клиента успешно обновлены.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // If linking to users, re-populate SelectList for UserId if necessary
            // Example: ViewBag.UserId = new SelectList(await _userManager.Users.OrderBy(u=>u.UserName).ToListAsync(), "Id", "UserName", client.UserId);
            return View(client);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .Include(c => c.User) // Include User details for display on delete confirmation
                .FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "Клиент успешно удален.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}
