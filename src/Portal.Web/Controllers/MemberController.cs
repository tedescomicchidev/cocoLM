using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Portal.Application;
using Portal.Domain;
using Portal.Infrastructure.Data;
using Portal.Web.Models;

namespace Portal.Web.Controllers;

[Authorize(Roles = "GlobalAdmin,OrgAdmin,OrgMember")]
public sealed class MemberController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RetrievalService _retrievalService;
    private readonly IngestionService _ingestionService;

    public MemberController(AppDbContext db, UserManager<ApplicationUser> userManager, RetrievalService retrievalService, IngestionService ingestionService)
    {
        _db = db;
        _userManager = userManager;
        _retrievalService = retrievalService;
        _ingestionService = ingestionService;
    }

    public async Task<IActionResult> Index(Guid? conversationId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.OrgId is null)
        {
            return Forbid();
        }
        var org = _db.Organizations.First(o => o.Id == user.OrgId.Value);
        var conversations = _db.Conversations.Where(c => c.OrgId == org.Id && c.UserId == user.Id).OrderByDescending(c => c.CreatedAt).ToList();
        var messages = conversationId.HasValue
            ? _db.Messages.Where(m => m.ConversationId == conversationId.Value).OrderBy(m => m.CreatedAt).ToList()
            : new List<Message>();
        var documents = _db.Documents.Where(d => d.OrgId == org.Id).OrderByDescending(d => d.UploadedAt).ToList();
        var audits = _db.RetrievalAudits.Where(a => a.OrgId == org.Id).OrderByDescending(a => a.Timestamp).Take(5).ToList();
        return View(new MemberPortalViewModel
        {
            OrgId = org.Id,
            OrgName = org.Name,
            Conversations = conversations,
            Messages = messages,
            Documents = documents,
            Audits = audits,
            CurrentConversationId = conversationId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Chat(ChatForm form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.OrgId is null)
        {
            return Forbid();
        }

        var response = await _retrievalService.ChatAsync(new ChatRequest(user.OrgId.Value, user.Id, form.Query, form.IncludeShared, form.PurposeTag, form.ConversationId));
        TempData["LastAnswer"] = response.Answer;
        TempData["Citations"] = System.Text.Json.JsonSerializer.Serialize(response.Citations);
        return RedirectToAction(nameof(Index), new { conversationId = response.ConversationId });
    }

    [HttpPost]
    public async Task<IActionResult> Upload(DocumentUploadForm form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.OrgId is null)
        {
            return Forbid();
        }

        if (form.File is null)
        {
            return RedirectToAction(nameof(Index));
        }

        await using var stream = form.File.OpenReadStream();
        await _ingestionService.UploadAsync(new UploadDocumentRequest(user.OrgId.Value, form.Title, form.File.FileName, form.File.ContentType, stream, user.Id));
        return RedirectToAction(nameof(Index));
    }
}
