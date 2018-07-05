using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Contents.ViewModels;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;

namespace OrchardCore.Contents.ViewComponents
{
    public class FilterBoxViewComponent : ViewComponent
    {
        private readonly IContentManager _contentManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FilterBoxViewComponent(
            IContentManager contentManager,
            IContentDefinitionManager contentDefinitionManager,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            IShapeFactory shapeFactory,
            IStringLocalizer<FilterBoxViewComponent> localizer)
        {
            _contentManager = contentManager;
            _contentDefinitionManager = contentDefinitionManager;
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;

            New = shapeFactory;
            T = localizer;
        }

        public dynamic New { get; set; }
        public IStringLocalizer T { get; }

        public async Task<IViewComponentResult> InvokeAsync(FilterBoxViewModel vm)
        {
            if ((vm == null) || (vm.Options == null))
            {
                return null;
            }

            var currentUser = _httpContextAccessor.HttpContext?.User;

            if (currentUser == null)
            {
                return null;
            }

            var viewModel = (await New.ViewModel())
                .Options(vm.Options)
                .ContentSorts(GetContentSortsSelectList(vm.Options.OrderBy))
                .SortDirections(GetSortDirectionsSelectList(vm.Options.SortDirection))
                .ContentStatuses(GetContentStatusesSelectList(vm.Options.ContentsStatus))
                .ContentTypes(await GetContentTypes(vm.Options.TypeName, currentUser));

            return View(viewModel);
        }


        private List<SelectListItem> GetContentStatusesSelectList(ContentsStatus selectedStatus)
        {
            var result = new List<SelectListItem>(){
                new SelectListItem() { Text = T["latest"].Value, Value = ContentsStatus.Latest.ToString()},
                new SelectListItem() { Text = T["owned by me"].Value, Value = ContentsStatus.Owner.ToString()},
                new SelectListItem() { Text = T["published"].Value, Value = ContentsStatus.Published.ToString()},
                new SelectListItem() { Text = T["unpublished"].Value, Value = ContentsStatus.Draft.ToString()},
                new SelectListItem() { Text = T["all versions"].Value, Value = ContentsStatus.AllVersions.ToString()}
            };

            result.Where(item => item.Value == selectedStatus.ToString()).FirstOrDefault().Selected = true;

            return result;
        }

        private List<SelectListItem> GetContentSortsSelectList(ContentsOrder selectedSort)
        {
            var result = new List<SelectListItem>() {
                new SelectListItem() { Text = T["recently created"].Value, Value = ContentsOrder.Created.ToString() },
                new SelectListItem() { Text = T["recently modified"].Value, Value = ContentsOrder.Modified.ToString() },
                new SelectListItem() { Text = T["recently published"].Value, Value = ContentsOrder.Published.ToString() }
            };
            result.Where(item => item.Value == selectedSort.ToString()).FirstOrDefault().Selected = true;

            return result;
        }

        private List<SelectListItem> GetSortDirectionsSelectList(SortDirection selectedSortDirection)
        {
            var result = new List<SelectListItem>() {
                new SelectListItem() { Text = T["Descending"].Value, Value = SortDirection.Descending.ToString() },
                new SelectListItem() { Text = T["Ascending"].Value, Value = SortDirection.Ascending.ToString() }                
            };
            result.Where(item => item.Value == selectedSortDirection.ToString()).FirstOrDefault().Selected = true;

            return result;
        }


        private async Task<List<SelectListItem>> GetContentTypes(string selectedType, ClaimsPrincipal user)
        {
            var result = new List<SelectListItem>();

            IEnumerable<ContentTypeDefinition> listable = (await GetListableTypesAsync(user)).ToList().OrderBy(ctd => ctd.Name);
            result.Add(new SelectListItem() { Text = T["All content types"], Value = "" });
            foreach (ContentTypeDefinition t in listable)
            {
                result.Add(new SelectListItem() { Text = t.DisplayName, Value = t.Name, Selected = t.Name == selectedType });
            }

            return result;
        }


        private async Task<IEnumerable<ContentTypeDefinition>> GetListableTypesAsync(ClaimsPrincipal user)
        {
            var listable = new List<ContentTypeDefinition>();
            foreach (var ctd in _contentDefinitionManager.ListTypeDefinitions())
            {
                if (ctd.Settings.ToObject<ContentTypeSettings>().Listable)
                {
                    var authorized = await _authorizationService.AuthorizeAsync(user, Permissions.EditContent, await _contentManager.NewAsync(ctd.Name));
                    if (authorized)
                    {
                        listable.Add(ctd);
                    }
                }
            }
            return listable;
        }
    }
}
