using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreBB.Web.Aspects;
using CoreBB.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreBB.Web.Controllers
{
    [Authorize]
    public class TopicController : Controller
    {
        private CoreBBContext _dbContext;

        public TopicController(CoreBBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        [LoggingAspect]
        public IActionResult Index(int forumId)
        {
            var forum = _dbContext.Forum.Include("Owner").SingleOrDefault(f => f.Id == forumId);
            if (forum == null)
            {
                throw new Exception("Forum does not exist.");
            }

            forum.Topic = _dbContext.Topic.Include("Owner")
                .Where(t => t.ForumId == forumId && t.ReplyToTopicId == null).ToList();
            return View(forum);
        }

        [HttpGet]
        [LoggingAspect]
        public IActionResult Create(int forumId)
        {
            var forum = _dbContext.Forum.SingleOrDefault(f => f.Id == forumId);
            if (forum == null)
            {
                throw new Exception("Forum does not exist.");
            }

            if (forum.IsLocked)
            {
                throw new Exception("This forum is locked.");
            }

            var topic = new Topic { ForumId = forumId };
            return View(topic);
        }

        [HttpPost]
        [LoggingAspect]
        public async Task<IActionResult> Create(Topic model)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Invalid topic information.");
            }

            var user = _dbContext.User.SingleOrDefault(u => u.Name == User.Identity.Name);
            model.OwnerId = user.Id;
            model.PostDateTime = DateTime.Now;
            await _dbContext.Topic.AddAsync(model);
            await _dbContext.SaveChangesAsync();
            model.RootTopicId = model.Id;
            _dbContext.Topic.Update(model);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", new { forumId = model.ForumId });
        }

        [HttpGet]
        [LoggingAspect]
        public IActionResult Detail(int id)
        {
            var rootTopic = _dbContext.Topic.Include("Owner").Include("ModifiedByUser")
                .SingleOrDefault(t => t.Id == id);
            if (rootTopic == null)
            {
                throw new Exception("Topic does not exist.");
            }

            rootTopic.InverseReplyToTopic = _dbContext.Topic.Include("Owner").Include("ModifiedByUser")
                .Where(t => t.RootTopicId == id && t.ReplyToTopicId != null).ToList();
            return View(rootTopic);
        }

        [HttpPost]
        [LoggingAspect]
        public async Task<IActionResult> Reply(Topic model)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Invalid topic information.");
            }

            var user = _dbContext.User.SingleOrDefault(u => u.Name == User.Identity.Name);
            model.OwnerId = user.Id;
            model.PostDateTime = DateTime.Now;
            await _dbContext.Topic.AddAsync(model);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", new { forumId = model.ForumId });
        }

        [HttpGet]
        [LoggingAspect]
        public IActionResult Reply(int toId)
        {
            var toTopic = _dbContext.Topic.SingleOrDefault(t => t.Id == toId);
            if (toTopic == null)
            {
                throw new Exception("The topic does not exist.");
            }

            if (toTopic.IsLocked)
            {
                throw new Exception("The topic is locked.");
            }

            var topic = new Topic
            {
                ReplyToTopicId = toTopic.Id,
                RootTopicId = toTopic.RootTopicId,
                ForumId = toTopic.ForumId,
                ReplyToTopic = toTopic
            };

            return View(topic);
        }

        [HttpGet]
        [LoggingAspect]
        public IActionResult Edit(int id)
        {
            var topic = _dbContext.Topic.Include("Owner").Include("ModifiedByUser")
                .SingleOrDefault(t => t.Id == id);
            if (topic == null)
            {
                throw new Exception("Topic does not exist.");
            }

            var user = _dbContext.User.SingleOrDefault(u => u.Name == User.Identity.Name);
            if (!(topic.OwnerId == user.Id || User.IsInRole(Roles.Administrator)))
            {
                throw new Exception("Update topic denied.");
            }

            return View(topic);
        }

        [HttpPost]
        [LoggingAspect]
        public async Task<IActionResult> Edit(Topic model)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Invalid topic information.");
            }

            var user = _dbContext.User.SingleOrDefault(u => u.Name == User.Identity.Name);
            model.ModifiedByUserId = user.Id;
            model.ModifyDateTime = DateTime.Now;
            _dbContext.Topic.Update(model);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", new { forumId = model.ForumId });
        }

        [HttpGet]
        [LoggingAspect]
        public IActionResult Delete(int id)
        {
            var topic = _dbContext.Topic.SingleOrDefault(t => t.Id == id);
            if (topic == null)
            {
                throw new Exception("Topic does not exist.");
            }

            var user = _dbContext.User.SingleOrDefault(u => u.Name == User.Identity.Name);
            if (!(topic.OwnerId == user.Id || User.IsInRole(Roles.Administrator)))
            {
                throw new Exception("You are not authorized to delete this topic.");
            }

            return View(topic);
        }

        [HttpPost]
        [LoggingAspect]
        public async Task<IActionResult> Delete(Topic model)
        {
            if (!ModelState.IsValid)
            {
                throw new Exception("Invalid topic information.");
            }

            var topic = _dbContext.Topic.SingleOrDefault(t => t.Id == model.Id);
            if (topic == null)
            {
                throw new Exception("Topic does not exist.");
            }

            var user = _dbContext.User.SingleOrDefault(u => u.Name == User.Identity.Name);
            if (!(topic.OwnerId == user.Id || User.IsInRole(Roles.Administrator)))
            {
                throw new Exception("You are not authorized to delete this topic.");
            }

            _dbContext.Topic.Remove(topic);
            if (topic.Id == topic.RootTopicId)
            {
                var descendants = _dbContext.Topic.Where(t => t.RootTopicId == topic.Id);
                _dbContext.Topic.RemoveRange(descendants);
            }
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", new { forumId = topic.ForumId });
        }
    }
}