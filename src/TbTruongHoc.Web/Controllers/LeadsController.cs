using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Piranha;
using TbTruongHoc.Web.Data;
using TbTruongHoc.Web.Models;
using TbTruongHoc.Web.Notifications;

namespace TbTruongHoc.Web.Controllers;

/// <summary>
/// Story 1.4 (FR-3): the single write path for every lead. A plain JSON API
/// controller - deliberately not shaped like <see cref="CmsController"/>
/// (which returns MVC views) - backing the <c>_QuoteRequestForm.cshtml</c>
/// partial's <c>fetch</c> submission.
///
/// Resolves the current <see cref="Piranha.Models.Site"/> the exact same way
/// Piranha's own request pipeline does
/// (<c>api.Sites.GetByHostnameAsync(host)</c> falling back to
/// <c>api.Sites.GetDefaultAsync()</c> - see <c>HostnameResolutionTests</c>)
/// rather than trusting any client-supplied site id, since this action isn't
/// covered by Piranha's own CMS routing middleware.
/// </summary>
[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    /// <summary>
    /// Closed set of accepted <see cref="FormSubmission.FormType"/> values -
    /// "general" (this story), "survey" (Story 6.5's future reuse of this
    /// same endpoint/table) and "landing" (Story 3.1's paid-ads landing
    /// page, <see cref="LandingPage.FormType"/>). Anything else the client
    /// sends, including blank, is coerced to "general" rather than persisted verbatim - see
    /// <see cref="LeadSubmissionRequest.FormType"/>'s doc comment, which
    /// already described this as a closed set.
    /// </summary>
    private static readonly HashSet<string> AllowedFormTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "general",
        "survey",
        LandingPage.FormType
    };

    private readonly IApi _api;
    private readonly LeadDbContext _leadDb;
    private readonly ILogger<LeadsController> _logger;
    private readonly IFormNotificationService _notifications;

    public LeadsController(IApi api, LeadDbContext leadDb, ILogger<LeadsController> logger, IFormNotificationService notifications)
    {
        _api = api;
        _leadDb = leadDb;
        _logger = logger;
        _notifications = notifications;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeadSubmissionRequest request)
    {
        // [ApiController] already short-circuits to an automatic 400
        // ValidationProblemDetails response (field name -> error messages)
        // when [Required]/[StringLength] on the DTO fails, before this
        // method body ever runs - EXCEPT when the body is a literal JSON
        // `null`, which binds `request` to null rather than failing
        // validation (this project has no <Nullable>enable</Nullable>, so
        // that automatic behavior doesn't kick in here). Guard explicitly.
        if (request is null)
        {
            return ValidationProblem();
        }

        var formType = AllowedFormTypes.Contains(request.FormType ?? string.Empty)
            ? request.FormType!.ToLowerInvariant()
            : "general";
        // Story 6.5 (FR-8): a survey's location is required (SurveyLocation
        // attribute, already enforced by the automatic 400 above) but never
        // blocks on *where* it is - out-of-area only warns.
        var isSurvey = formType == "survey";

        FormSubmission savedSubmission;

        try
        {
            var host = Request.Host.Host;
            var site = await _api.Sites.GetByHostnameAsync(host) ?? await _api.Sites.GetDefaultAsync();

            if (site == null)
            {
                // No Site record exists at all (shouldn't happen once Story
                // 1.1's seed has run) - fail closed rather than saving a lead
                // with an empty/garbage SiteId.
                _logger.LogError("Rejecting lead submission for host {Host}: no Site record resolved (not even the default site).", host);
                return Problem(
                    detail: "No site is configured to receive this submission.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                SiteId = site.Id,
                FormType = formType,
                Name = request.Name!.Trim(),
                Phone = request.Phone!.Trim(),
                ProductOfInterest = string.IsNullOrWhiteSpace(request.ProductOfInterest) ? null : request.ProductOfInterest!.Trim(),
                Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message!.Trim(),
                // Story 6.5: survey-only; the flag is always the server's own reading.
                LocationAddress = isSurvey ? request.LocationAddress!.Trim() : null,
                IsOutsideServiceArea = isSurvey ? ServiceArea.IsOutside(request.LocationAddress) : null,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _leadDb.FormSubmissions.Add(submission);
            await _leadDb.SaveChangesAsync();

            savedSubmission = submission;
        }
        catch (Exception ex)
        {
            // DB-unreachable/server-error case from the I/O matrix (covers
            // both site resolution and SaveChangesAsync, since either can
            // fail the same way when the database is unreachable): never let
            // an unhandled exception reach the visitor, and never leave a
            // half-saved row - EF Core's SaveChangesAsync is already
            // all-or-nothing per call, so nothing further to roll back.
            _logger.LogError(ex, "Failed to save a lead submission.");
            return Problem(
                detail: "We couldn't save your request right now. Please call or message us on Zalo instead.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        // Story 1.7 (FR-3, AD-3): notify only after the row is committed, and
        // deliberately outside the try/catch above - a notification failure
        // must never turn an already-saved lead into a 500. The service only
        // queues (never waits on SMTP) and never throws; the extra guard here
        // keeps the visitor's 200 intact even if a future implementation did.
        try
        {
            _notifications.NotifyNewSubmission(savedSubmission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue the notification for lead {SubmissionId}; the lead itself is saved.", savedSubmission.Id);
        }

        return Ok(new { id = savedSubmission.Id });
    }
}
