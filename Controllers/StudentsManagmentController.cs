using DatabaseContext;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Kafedra.Controllers
{
    [ApiController]
    [Route("/StudentsManagment")]
    public class StudentsManagmentController : Controller
    {
        [HttpGet]
        public ViewResult LoadPage()
        {
            return View("StudentsList");
        }

        [HttpGet("UpdateList")]
        public IActionResult UpdateList()
        {
            var token = Request.Headers["Authorization"];
            if (!Hash.IsUserExists(Hash.GetUsernameFromJwtToken(token)))
            {
                return Redirect("/");
            }

            var list = AppDbContext.Instance.Students.Select(student => new
            {
                name = student.StudentName,
                status = (int)student.Status,
                group = student.Group
            }).ToList();

            return Ok(list);
        }

        [HttpPost("UpdateStudent")]
        public IActionResult UpdateStudent([FromBody] UpdateRequest updateRequest)
        {
            var token = Request.Headers["Authorization"];
            var username = Hash.GetUsernameFromJwtToken(token);
            if (!Hash.IsUserExists(username))
                return Redirect("/");

            var user = AppDbContext.Instance.Users.Where(u => u.Username == username).FirstOrDefault();
            if (user != null && user.IsStudent)
                return Redirect("/");

            var student = AppDbContext.Instance.Students.Where(s => s.StudentName == updateRequest.studentName).FirstOrDefault();
            if (user == null)
                return Redirect("/");

            student.Status = (StudentStatus)updateRequest.status;
            student.Group = updateRequest.group;
            AppDbContext.Instance.SaveChanges();

            return Ok();
        }

        [HttpGet("/studentPage")]
        public ViewResult GetStudentPage() 
        {
            return View("StudentPage");
        }

        [HttpGet("/studentPage/UpdateList")]
        public IActionResult UpdateStudentInfo()
        {
            var token = Request.Headers["Authorization"];
            var username = Hash.GetUsernameFromJwtToken(token);
            if (!Hash.IsUserExists(username))
                return Redirect("/");

            var user = AppDbContext.Instance.Students.Where(u => u.StudentName == username).FirstOrDefault();
            if (user == null)
                return Redirect("/");

            return Ok(new {
                name = user.StudentName,
                status = (int)user.Status,
                group = user.Group
            });
        }

        public class UpdateRequest
        {
            public string studentName { get; set; }
            public int status { get; set; }
            public string group { get; set; }
        }
    }
}
