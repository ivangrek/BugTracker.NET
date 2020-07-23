/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using Core;
    using Core.Controls;
    using BugTracker.Web.Core.Persistence;
    using Models;
    using Models.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using Identification.Querying;
    using Querying;
    using Tracking.Querying.Organizations;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class QueryController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IQueryService queryService;
        private readonly ApplicationContext applicationContext;
        private readonly IApplicationFacade applicationFacade;
        private readonly IQueryBuilder queryBuilder;

        public QueryController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IQueryService queryService,
            ApplicationContext applicationContext,
            IApplicationFacade applicationFacade,
            IQueryBuilder queryBuilder)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.queryService = queryService;
            this.applicationContext = applicationContext;
            this.applicationFacade = applicationFacade;
            this.queryBuilder = queryBuilder;
        }

        [HttpGet]
        public ActionResult Index(bool? showAll)
        {
            var isAuthorized = this.security.User.IsAdmin
                 || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - queries",
                SelectedItem = MainMenuSections.Queries
            };

            var result = this.queryService
                .LoadList(showAll ?? false)
                .Tables[0];

            ViewBag.ShowAll = showAll ?? false;

            return View(result);
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - create query",
                SelectedItem = MainMenuSections.Queries
            };

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var userQuery = this.queryBuilder
                .From<IUserSource>()
                .To<IUserComboBoxResult>()
                .Sort()
                    .AscendingBy(x => x.Name)
                .Build();

            var userResult = this.applicationFacade
                .Run(userQuery);

            ViewBag.Users = userResult
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToList();

            ViewBag.Users.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "[select user]"
            });

            var organizationQuery = this.queryBuilder
                .From<IOrganizationSource>()
                .To<IOrganizationComboBoxResult>()
                .Sort()
                    .AscendingBy(x => x.Name)
                .Build();

            var organizationResult = this.applicationFacade
                .Run(organizationQuery);

            ViewBag.Organizations = organizationResult
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToList();

            ViewBag.Organizations.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "[select org]"
            });

            var model = new EditModel
            {
                Visibility = 0, // these guys can do everything
                SqlText = HttpUtility.HtmlDecode(Request["sql_text"]) // if coming from Search.aspx
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            if (model.Visibility == 2)
            {
                if (model.QrganizationId < 1)
                {
                    ModelState.AddModelError(nameof(EditModel.Visibility), "You must select a organization.");
                }
            }
            else if (model.Visibility == 1)
            {
                if (model.UserId < 1)
                {
                    ModelState.AddModelError(nameof(EditModel.Visibility), "You must select a user.");
                }
            }

            // See if name is already used?
            var queryCount = this.applicationContext.Queries
                .Count(x => x.Name == model.Name);

            if (queryCount > 0)
            {
                ModelState.AddModelError(nameof(EditModel.Name), "A query with this name already exists. Choose another name.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Query was not created.");

                var userQuery = this.queryBuilder
                    .From<IUserSource>()
                    .To<IUserComboBoxResult>()
                    .Sort()
                    .AscendingBy(x => x.Name)
                    .Build();

                var userResult = this.applicationFacade
                    .Run(userQuery);

                ViewBag.Users = userResult
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToList();

                ViewBag.Users.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[select user]"
                });

                var organizationQuery = this.queryBuilder
                    .From<IOrganizationSource>()
                    .To<IOrganizationComboBoxResult>()
                    .Sort()
                    .AscendingBy(x => x.Name)
                    .Build();

                var organizationResult = this.applicationFacade
                    .Run(organizationQuery);

                ViewBag.Organizations = organizationResult
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToList();

                ViewBag.Organizations.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[select org]"
                });

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create query",
                    SelectedItem = MainMenuSections.Queries
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString()},
                { "$de", model.Name },
                { "$sq", string.Empty },
                { "$us", string.Empty },
                { "$rl", string.Empty },
            };

            //if (Util.GetSetting("HtmlEncodeSql","0") == "1")
            //{
            //  sql = sql.Replace("$sq", Server.HtmlDecode(sql_text.Value.Replace("'","''")));
            //}
            //else
            //{
            parameters["$sq"] = model.SqlText;
            //}

            if (model.Visibility == 0)
            {
                parameters["$us"] = "0";
                parameters["$rl"] = "0";
            }
            else if (model.Visibility == 1)
            {
                parameters["$us"] = Convert.ToString(model.UserId);
                parameters["$rl"] = "0";
            }
            else
            {
                parameters["$us"] = "0";
                parameters["$rl"] = Convert.ToString(model.QrganizationId);
            }

            this.queryService.Create(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - update query",
                SelectedItem = MainMenuSections.Queries
            };

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var userQuery = this.queryBuilder
                .From<IUserSource>()
                .To<IUserComboBoxResult>()
                .Sort()
                .AscendingBy(x => x.Name)
                .Build();

            var userResult = this.applicationFacade
                .Run(userQuery);

            ViewBag.Users = userResult
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToList();

            ViewBag.Users.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "[select user]"
            });

            var organizationQuery = this.queryBuilder
                .From<IOrganizationSource>()
                .To<IOrganizationComboBoxResult>()
                .Sort()
                .AscendingBy(x => x.Name)
                .Build();

            var organizationResult = this.applicationFacade
                .Run(organizationQuery);

            ViewBag.Organizations = organizationResult
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToList();

            ViewBag.Organizations.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "[select org]"
            });

            // Get this entry's data from the db and fill in the form
            var dataRow = this.queryService.LoadOne(id);

            if (dataRow.UserId != this.security.User.Usid)
            {
                if (!isAuthorized)
                {
                    return Content("You are not allowed to edit this query");
                }
            }

            var model = new EditModel
            {
                Id = id,
                Name = dataRow.Name,
                Visibility = 0, // these guys can do everything
                SqlText = dataRow.Sql
            };

            if ((dataRow.UserId == null || dataRow.UserId.Value == 0) && (dataRow.OrganizationId == null || dataRow.OrganizationId.Value == 0))
            {
                model.Visibility = 0;
            }
            else if (dataRow.UserId > 0)
            {
                model.Visibility = 1;
                model.UserId = dataRow.UserId ?? 0;
            }
            else
            {
                model.Visibility = 2;
                model.QrganizationId = dataRow.OrganizationId ?? 0;
            }

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            if (model.Visibility == 2)
            {
                if (model.QrganizationId < 1)
                {
                    ModelState.AddModelError(nameof(EditModel.Visibility), "You must select a organization.");
                }
            }
            else if (model.Visibility == 1)
            {
                if (model.UserId < 1)
                {
                    ModelState.AddModelError(nameof(EditModel.Visibility), "You must select a user.");
                }
            }

            // See if name is already used?
            var queryCount = this.applicationContext.Queries
                .Where(x => x.Name == model.Name)
                .Count(x => x.Id != model.Id);

            if (queryCount > 0)
            {
                ModelState.AddModelError(nameof(EditModel.Name), "A query with this name already exists. Choose another name.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Query was not updated.");

                var userQuery = this.queryBuilder
                    .From<IUserSource>()
                    .To<IUserComboBoxResult>()
                    .Sort()
                    .AscendingBy(x => x.Name)
                    .Build();

                var userResult = this.applicationFacade
                    .Run(userQuery);

                ViewBag.Users = userResult
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToList();

                ViewBag.Users.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[select user]"
                });

                var organizationQuery = this.queryBuilder
                    .From<IOrganizationSource>()
                    .To<IOrganizationComboBoxResult>()
                    .Sort()
                    .AscendingBy(x => x.Name)
                    .Build();

                var organizationResult = this.applicationFacade
                    .Run(organizationQuery);

                ViewBag.Organizations = organizationResult
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToList();

                ViewBag.Organizations.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[select org]"
                });

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - update query",
                    SelectedItem = MainMenuSections.Queries
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString()},
                { "$de", model.Name },
                { "$sq", string.Empty },
                { "$us", string.Empty },
                { "$rl", string.Empty },
            };

            //if (Util.GetSetting("HtmlEncodeSql","0") == "1")
            //{
            //  sql = sql.Replace("$sq", Server.HtmlDecode(sql_text.Value.Replace("'","''")));
            //}
            //else
            //{
            parameters["$sq"] = model.SqlText;
            //}

            if (model.Visibility == 0)
            {
                parameters["$us"] = "0";
                parameters["$rl"] = "0";
            }
            else if (model.Visibility == 1)
            {
                parameters["$us"] = Convert.ToString(model.UserId);
                parameters["$rl"] = "0";
            }
            else
            {
                parameters["$us"] = "0";
                parameters["$rl"] = Convert.ToString(model.QrganizationId);
            }

            this.queryService.Update(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var (valid, name) = this.queryService.CheckDeleting(id);

            if (!valid && !isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete query",
                SelectedItem = MainMenuSections.Queries
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var (valid, name) = this.queryService.CheckDeleting(model.Id);

            if (!valid && !isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            // do delete here
            this.queryService.Delete(model.Id);

            return RedirectToAction(nameof(Index));
        }
    }
}