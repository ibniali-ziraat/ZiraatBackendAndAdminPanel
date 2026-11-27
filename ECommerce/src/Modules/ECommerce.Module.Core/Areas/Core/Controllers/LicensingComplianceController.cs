using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [ApiController]
    [Route("api/core/[controller]")]
    public class LicensingComplianceController : ControllerBase
    {
        private readonly ILicensingComplianceService _service;

        public LicensingComplianceController(ILicensingComplianceService service) => _service = service;

        #region Compliance Operations

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _service.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving licensing compliance records", error = ex.Message });
            }
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<LicensingCompliance>> GetById(long id)
        {
            try
            {
                var item = await _service.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { message = $"Licensing compliance record with Id {id} not found" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving licensing compliance record", error = ex.Message });
            }
        }

        [HttpGet("supplier/{supplierProfileId:long}")]
        public async Task<ActionResult<LicensingCompliance>> GetBySupplierProfileId(long supplierProfileId)
        {
            try
            {
                var item = await _service.GetBySupplierProfileIdAsync(supplierProfileId);
                if (item == null)
                {
                    return NotFound(new { message = $"Licensing compliance record for supplier {supplierProfileId} not found" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving licensing compliance record", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LicensingComplianceViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var entity = new LicensingCompliance
                {
                    SupplierProfileId = model.SupplierProfileId,
                    GSTNumber = model.GSTNumber,
                    PANNumber = model.PANNumber
                };

                if (model.Licenses != null && model.Licenses.Count > 0)
                {
                    foreach (var licenseVm in model.Licenses)
                    {
                        var license = MapLicenseViewModelToEntity(licenseVm);
                        entity.Licenses.Add(license);
                    }
                }

                await _service.AddAsync(entity);

                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new { id = entity.Id, data = entity });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating licensing compliance record", error = ex.Message });
            }
        }

        [HttpPatch("{id:long}")]
        public async Task<IActionResult> UpdateCompliance(long id, [FromBody] LicensingComplianceViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existing = await _service.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = $"Licensing compliance record with Id {id} not found" });
                }

                existing.GSTNumber = model.GSTNumber ?? existing.GSTNumber;
                existing.PANNumber = model.PANNumber ?? existing.PANNumber;

                if (model.Licenses != null && model.Licenses.Count > 0)
                {
                    await UpdateLicensesAsync(existing.Id, model.Licenses);
                }

                await _service.UpdateAsync(existing);

                return Ok(new { message = "Licensing compliance record updated successfully", id = existing.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating licensing compliance record", error = ex.Message });
            }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var existing = await _service.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = $"Licensing compliance record with Id {id} not found" });
                }

                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { message = "Failed to delete licensing compliance record" });
                }

                return Ok(new { message = "Licensing compliance record and all associated licenses and documents deleted successfully", id = id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error deleting licensing compliance record", error = ex.Message });
            }
        }

        #endregion

        #region License Operations

        [HttpGet("compliance/{complianceId:long}/licenses")]
        public async Task<ActionResult> GetLicensesByComplianceId(long complianceId)
        {
            try
            {
                var licenses = await _service.GetLicensesByComplianceIdAsync(complianceId);
                return Ok(licenses);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving licenses", error = ex.Message });
            }
        }

        [HttpGet("license/{licenseId:long}")]
        public async Task<ActionResult<License>> GetLicenseById(long licenseId)
        {
            try
            {
                var license = await _service.GetLicenseByIdAsync(licenseId);
                if (license == null)
                {
                    return NotFound(new { message = $"License with Id {licenseId} not found" });
                }

                return Ok(license);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving license", error = ex.Message });
            }
        }

        /// <summary>
        /// Add a new license to an existing compliance record
        /// </summary>
        [HttpPost("compliance/{complianceId:long}/licenses")]
        public async Task<IActionResult> AddLicense(long complianceId, [FromBody] LicenseViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var license = new License
                {
                    LicenseType = model.LicenseType,
                    LicenseNumber = model.LicenseNumber,
                    StartDate = model.StartDate,
                    ExpiryDate = model.ExpiryDate,
                    LicenseDocument = model.LicenseDocument != null ? new LicenseDocument
                    {
                        FileName = model.LicenseDocument.FileName,
                        FileUrl = model.LicenseDocument.FileUrl
                    } : null
                };

                if (model.AdditionalDocuments != null && model.AdditionalDocuments.Count > 0)
                {
                    foreach (var docVm in model.AdditionalDocuments)
                    {
                        license.AdditionalDocuments.Add(new AdditionalDocument
                        {
                            DocumentType = docVm.DocumentType,
                            FileName = docVm.FileName,
                            FileUrl = docVm.FileUrl
                        });
                    }
                }

                var addedLicense = await _service.AddLicenseAsync(complianceId, license);

                return CreatedAtAction(nameof(GetLicenseById), new { licenseId = addedLicense.Id }, addedLicense);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error adding license", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing license
        /// </summary>
        [HttpPut("license/{licenseId:long}")]
        public async Task<IActionResult> UpdateLicense(long licenseId, [FromBody] LicenseViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var license = await _service.GetLicenseByIdAsync(licenseId);
                if (license == null)
                {
                    return NotFound(new { message = $"License with Id {licenseId} not found" });
                }

                license.LicenseType = model.LicenseType ?? license.LicenseType;
                license.LicenseNumber = model.LicenseNumber ?? license.LicenseNumber;
                license.StartDate = model.StartDate ?? license.StartDate;
                license.ExpiryDate = model.ExpiryDate ?? license.ExpiryDate;

                if (model.LicenseDocument != null)
                {
                    if (license.LicenseDocument == null)
                    {
                        license.LicenseDocument = new LicenseDocument();
                    }

                    license.LicenseDocument.FileName = model.LicenseDocument.FileName ?? license.LicenseDocument.FileName;
                    license.LicenseDocument.FileUrl = model.LicenseDocument.FileUrl ?? license.LicenseDocument.FileUrl;
                }

                if (model.AdditionalDocuments != null)
                {
                    UpdateAdditionalDocumentsForLicense(license, model.AdditionalDocuments);
                }

                await _service.UpdateLicenseAsync(license);

                return Ok(new { message = "License updated successfully", id = license.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating license", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a license and all its associated documents (LicenseDocument and AdditionalDocuments)
        /// </summary>
        [HttpDelete("license/{licenseId:long}")]
        public async Task<IActionResult> DeleteLicense(long licenseId)
        {
            try
            {
                var license = await _service.GetLicenseByIdAsync(licenseId);
                if (license == null)
                {
                    return NotFound(new { message = $"License with Id {licenseId} not found" });
                }

                var result = await _service.DeleteLicenseAsync(licenseId);
                if (!result)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Failed to delete license" });
                }

                return Ok(new { message = "License and all associated documents deleted successfully", licenseId = licenseId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error deleting license", error = ex.Message });
            }
        }

        #endregion

        #region Additional Document Operations

        /// <summary>
        /// Delete a single additional document from a license
        /// </summary>
        [HttpDelete("document/{documentId:long}")]
        public async Task<IActionResult> DeleteAdditionalDocument(long documentId)
        {
            try
            {
                var result = await _service.DeleteAdditionalDocumentAsync(documentId);
                if (!result)
                {
                    return NotFound(new { message = $"Additional document with Id {documentId} not found" });
                }

                return Ok(new { message = "Additional document deleted successfully", documentId = documentId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Error deleting additional document", error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private License MapLicenseViewModelToEntity(LicenseViewModel licenseVm)
        {
            var license = new License
            {
                LicenseType = licenseVm.LicenseType,
                LicenseNumber = licenseVm.LicenseNumber,
                StartDate = licenseVm.StartDate,
                ExpiryDate = licenseVm.ExpiryDate,
                LicenseDocument = licenseVm.LicenseDocument != null ? new LicenseDocument
                {
                    FileName = licenseVm.LicenseDocument.FileName,
                    FileUrl = licenseVm.LicenseDocument.FileUrl
                } : null
            };

            if (licenseVm.AdditionalDocuments != null && licenseVm.AdditionalDocuments.Count > 0)
            {
                foreach (var docVm in licenseVm.AdditionalDocuments)
                {
                    license.AdditionalDocuments.Add(new AdditionalDocument
                    {
                        DocumentType = docVm.DocumentType,
                        FileName = docVm.FileName,
                        FileUrl = docVm.FileUrl
                    });
                }
            }

            return license;
        }

        private async Task UpdateLicensesAsync(long complianceId, ICollection<LicenseViewModel> licenseViewModels)
        {
            var existingLicenses = (await _service.GetLicensesByComplianceIdAsync(complianceId)).ToList();

            foreach (var licenseVm in licenseViewModels)
            {
                License license;

                if (licenseVm.LicenseId > 0)
                {
                    // Update existing license
                    license = existingLicenses.FirstOrDefault(l => l.Id == licenseVm.LicenseId);
                    if (license == null)
                    {
                        continue;
                    }

                    license.LicenseType = licenseVm.LicenseType ?? license.LicenseType;
                    license.LicenseNumber = licenseVm.LicenseNumber ?? license.LicenseNumber;
                    license.StartDate = licenseVm.StartDate ?? license.StartDate;
                    license.ExpiryDate = licenseVm.ExpiryDate ?? license.ExpiryDate;

                    if (licenseVm.LicenseDocument != null)
                    {
                        if (license.LicenseDocument == null)
                        {
                            license.LicenseDocument = new LicenseDocument();
                        }

                        license.LicenseDocument.FileName = licenseVm.LicenseDocument.FileName ?? license.LicenseDocument.FileName;
                        license.LicenseDocument.FileUrl = licenseVm.LicenseDocument.FileUrl ?? license.LicenseDocument.FileUrl;
                    }

                    if (licenseVm.AdditionalDocuments != null)
                    {
                        UpdateAdditionalDocumentsForLicense(license, licenseVm.AdditionalDocuments);
                    }

                    await _service.UpdateLicenseAsync(license);
                }
                else if (licenseVm.LicenseId == 0 || licenseVm.LicenseId < 0)
                {
                    // Add new license
                    license = MapLicenseViewModelToEntity(licenseVm);
                    license.LicensingComplianceId = complianceId;
                    await _service.AddLicenseAsync(complianceId, license);
                }
            }
        }

        private void UpdateAdditionalDocumentsForLicense(License license, ICollection<AdditionalDocumentViewModel> docViewModels)
        {
            var remaining = license.AdditionalDocuments.ToList();
            var changes = new List<(AdditionalDocumentViewModel Model, AdditionalDocument Entity)>();

            // Validate and match the entire selection before removing or changing any documents.
            foreach (var docVm in docViewModels)
            {
                if (docVm == null || docVm.Id < 0)
                {
                    throw new ArgumentException("Invalid supporting document.");
                }

                AdditionalDocument existingDoc;
                if (docVm.Id > 0)
                {
                    existingDoc = remaining.FirstOrDefault(d => d.Id == docVm.Id);
                    if (existingDoc == null)
                    {
                        throw new ArgumentException("Supporting document ID is duplicated or does not belong to this license.");
                    }
                }
                else
                {
                    var matches = remaining.Where(d => d.DocumentType == docVm.DocumentType).ToList();
                    existingDoc = matches.FirstOrDefault(d => d.FileUrl == docVm.FileUrl)
                        ?? (matches.Count == 1 ? matches[0] : null);
                }

                if (existingDoc == null && string.IsNullOrWhiteSpace(docVm.DocumentType))
                {
                    throw new ArgumentException("Document type is required for a new supporting document.");
                }

                if (existingDoc != null)
                {
                    remaining.Remove(existingDoc);
                }
                changes.Add((docVm, existingDoc));
            }

            foreach (var change in changes)
            {
                var docVm = change.Model;
                var existingDoc = change.Entity;
                if (existingDoc != null)
                {
                    existingDoc.DocumentType = docVm.DocumentType ?? existingDoc.DocumentType;
                    existingDoc.FileName = docVm.FileName ?? existingDoc.FileName;
                    existingDoc.FileUrl = docVm.FileUrl ?? existingDoc.FileUrl;
                }
                else
                {
                    // Add new additional document
                    license.AdditionalDocuments.Add(new AdditionalDocument
                    {
                        DocumentType = docVm.DocumentType,
                        FileName = docVm.FileName,
                        FileUrl = docVm.FileUrl
                    });
                }
            }

            foreach (var removed in remaining)
            {
                _service.RemoveAdditionalDocument(removed);
                license.AdditionalDocuments.Remove(removed);
            }
        }

        #endregion
    }
}
