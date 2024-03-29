﻿using Entities;
using Entities.Account;
using Entities.JoinTables;
using Entities.ProjectEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceContracts;
using ServiceContracts.DTO.ProjectDTO;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Services.ProjectServices
{
    public class ProjectService : IProjectService
    {
        private readonly PMS_DBContext _db;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUsersService _userService;
        public ProjectService(PMS_DBContext pMS_DBContext, SignInManager<ApplicationUser> signInManager, IUsersService userService)
        {
            _db = pMS_DBContext;
            _signInManager = _signInManager;
            _userService = userService;
        }

        public async Task<Project> AddProjectAsync(ProjectAddRequest? projectAddRequest, ClaimsPrincipal user)
        {
            Project project = projectAddRequest.ToProject();
            var userName = user.Identity.Name;
            project.ProjectWoner = userName;
            project.ApplicationUserProjects = new List<ApplicationUserProject>();
            var applicationUsers =await  _userService.GetApplicationUsersByTeamId((int)projectAddRequest.TeamId);
            var projectLead = await _userService.GetUserByUserName(projectAddRequest.ProjectLead);
            foreach(var  applicationUser in applicationUsers)
            {
                var userProject = new ApplicationUserProject()
                {
                    ApplicationUser = applicationUser,
                    Project = project,
                };
                project.ApplicationUserProjects.Add(userProject);
            }
            if(!applicationUsers.Contains(projectLead))
            {
                var leadProject = new ApplicationUserProject()
                {
                    ApplicationUser = projectLead,
                    Project = project,
                };
                project.ApplicationUserProjects.Add(leadProject);

            }
            project.LastVisitedTime=DateTime.Now;
            Board board = new Board()
            {
                Id=Guid.NewGuid(),
                Name="Default Board",
                Description="This is default Board for Every project",
                ProjectLead=userName,
                ProjectId=project.Id,
                
            };
            Sprint sprint1 = new Sprint()
            {
                Id = Guid.NewGuid(),
                Name = "Sprint1",
                Description="This is primary Description",
                BoardId=board.Id,
            };
            board.Sprints=new List<Sprint>() { sprint1};
            board.Backlog = new Backlog()
            {
                Id = Guid.NewGuid(),
                boardId = board.Id
            };
           
            
            
            project.Boards = new List<Board>() { board};

            _db.Projects.Add(project);
            _db.SaveChanges();
            return project;
  
        }
        public async Task<List<ProjectResponse>> GetProjectsByWoner(ClaimsPrincipal user)
        {
            var userName = user.Identity.Name;
            ICollection<Project> projects=await  _db.Projects.Where(c=>c.ProjectWoner==userName).ToListAsync();
            List<ProjectResponse> projectResponses = new List<ProjectResponse>();
            foreach(var project in projects)
            {
                projectResponses.Add(ConvertProjectToProjectResponse(project));
            }
            return projectResponses;
        }

        public async Task<ProjectResponse> GetProjectByIdAsync(Guid id)
        {
            var project = await _db.Projects.SingleOrDefaultAsync(c => c.Id==id);
            return project.ToProjectResponse();
        }
        
        public async Task<ProjectResponse> UpdateProjectByIdAsync( ProjectUpdateRequest projectUpdateRequest)
        {
            Project project = await _db.Projects.SingleOrDefaultAsync(c=>c.Id==projectUpdateRequest.Id);
            project.ProjectLead=projectUpdateRequest.ProjectLead;
           // project.ProjectWoner=projectUpdateRequest.ProjectWoner;
            project.ProjectName=projectUpdateRequest.ProjectName;
            project.Catagory=projectUpdateRequest.Catagory;
            project.DefaultAssignee=projectUpdateRequest.DefaultAssignee;
            await _db.SaveChangesAsync();
            return project.ToProjectResponse() ;
        }

        /// <summary>
        /// Delete Project By Id(type Guid)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ProjectResponse> DeleteProjectAsync(Guid id)
        {
            Project project =await _db.Projects.FirstOrDefaultAsync(i => i.Id == id);
            if (project != null)
            {
                _db.Projects.Remove(project);
                _db.SaveChanges();
            }
            return project.ToProjectResponse();
        }
        public async Task<Project> LaunchProject(Guid projectId)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(project=>project.Id==projectId);
            project.LastVisitedTime= DateTime.Now;
            await _db.SaveChangesAsync();
            return project;
        }
        private ProjectResponse ConvertProjectToProjectResponse(Project project)
        {
            ProjectResponse projectResponse = project.ToProjectResponse();
            return projectResponse;
        }

        public async Task<List<ProjectResponse>> GetProjectsOfGeneraluser(string userId,string currentUserName)
        {
            var projects =await _db.ApplicationUserProjects
            .Where(up => up.ApplicationUserId == userId )
            .Select(up => up.Project)
            .Where(p=>p.ProjectWoner!=currentUserName && p.ProjectLead!=currentUserName)
            .ToListAsync();
            List<ProjectResponse> projectResponses = new List<ProjectResponse>();
            foreach (var project in projects)
            {
                projectResponses.Add(project.ToProjectResponse());
            }

            return projectResponses;
        }

        public async Task<List<ProjectResponse>> GetProjectsOfProjectLead(string userId, string currentUserName)
        {
            var projects = await _db.ApplicationUserProjects
            .Where(up => up.ApplicationUserId == userId)
            .Select(up => up.Project)
            .Where(p =>  p.ProjectLead == currentUserName)
            .ToListAsync();
            List<ProjectResponse> projectResponses = new List<ProjectResponse>();
            foreach (var project in projects)
            {
                projectResponses.Add(project.ToProjectResponse());
            }

            return projectResponses;
        }
    }
}
