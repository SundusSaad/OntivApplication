using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Neo4jClient;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IGraphClient _client;
        public UserController(IGraphClient client)
        {
            _client = client;
        }


        //Get All Users by label
        [HttpGet]
        public async Task<ActionResult> GetUser()
        {
            var users = await _client.Cypher
            .Match("(user:User)")
            .Return(user => user.As<AppUser>())
            .ResultsAsync;

            return Ok(users);

        }

        //Get a Specific User by Id
        [HttpGet("/GetSpecificUserById")]
        public async Task<ActionResult> GetUserbyId(int id)
        {
            var user = await _client.Cypher
            .Match("(user:User)")
            .Where((AppUser user) => user.Id == id)
            .Return(user => user.As<AppUser>())
            .ResultsAsync;

            return Ok(user);
        }


        // Get a User and All other freinds and Count of their relation Employee
        [HttpGet("/GetUserWithRelation")]
        public async Task<ActionResult> GetEmployees(string UserName)
        {
            string username = UserName.ToLower();
            var user = await _client.Cypher
            .OptionalMatch("(user:User)-[EMPLOYEES_WITH]-(Employee:User)")
            .Where((AppUser user) => user.Name == username)
            .Return((user, Employee) => new
            {
                User = user.As<AppUser>(),
                Employee = Employee.CollectAs<AppUser>(),
                NumberOfFriends = Employee.Count()
            }).ResultsAsync;

            return Ok(user);
        }

        //Create a User
        [HttpPost("/CreateUser")]
        public async Task<ActionResult> CreateNewUser([FromBody] AppUser newUser)
        {
            AppUser NewUser = new AppUser()
            {
                Id = newUser.Id,
                Name = newUser.Name.ToLower(),
                Age = newUser.Age,
                Email = newUser.Email
            };
            await _client.Cypher
            .Create("(user: User $NewUser)")
            .WithParam("NewUser", NewUser)
            .ExecuteWithoutResultsAsync();

            return Ok();
        }
        //Create a User only it it doesnot exist
        [HttpPost("/CreateUserIfNotExist")]
        public async Task<ActionResult> CreateIfNotExist([FromBody] AppUser newUser)
        {
            AppUser NewUser = new AppUser()
            {
                Id = newUser.Id,
                Name = newUser.Name.ToLower(),
                Age = newUser.Age,
                Email = newUser.Email
            };
            await _client.Cypher
            .Merge("(user: User {Id :$NewUser.Id})")
            .OnCreate()
            .Set("user = $NewUser")
            .WithParams(new
            {
                id = newUser.Id,
                NewUser
            })
            .ExecuteWithoutResultsAsync();

            return Ok();
        }

        //Create a user and relate them to an existing one
        [HttpPost("/CreateUserAndRelatesToExisting")]
        public async Task<ActionResult> CreateRelation([FromBody] AppUser newUser, string UserName)
        {
            AppUser NewUser = new AppUser()
            {
                Id = newUser.Id,
                Name = newUser.Name.ToLower(),
                Age = newUser.Age,
                Email = newUser.Email
            };
            string username = UserName.ToLower();
            await _client.Cypher
            .Match("(user1:User)")
            .Where((AppUser user1) => user1.Name == username)
            .Create("(user1)-[:EMPLOYEES_WITH]->(user2:User $NewUser)")
            .WithParam("NewUser", NewUser)
            .ExecuteWithoutResultsAsync();


            return Ok();
        }

        //Relates two Existing Users
        [HttpPost("/RelatesExistingUser")]
        public async Task<ActionResult> CreateRelationExistingUser(string UserName1, string UserName2)
        {
            string username1 = UserName1.ToLower();
            string username2 = UserName2.ToLower();
            await _client.Cypher
            .Match("(user1:User)", "(user2:User)")
            .Where((AppUser user1) => user1.Name == username1)
            .AndWhere((AppUser user2) => user2.Name == username2)
            .Create("(user1)-[:WORKS_WITH]->(user2)")
            .ExecuteWithoutResultsAsync();


            return Ok();
        }

        //Relates two Existing Users only if they are not Related already
        [HttpPost("/RelatesExistingUserIfNotRelated")]
        public async Task<ActionResult> CreateIfNoRelationExistingUser(string UserName1, string UserName2)
        {
            string username1 = UserName1.ToLower();
            string username2 = UserName2.ToLower();
            await _client.Cypher
            .Match("(user1:User)", "(user2:User)")
            .Where((AppUser user1) => user1.Name == username1)
            .AndWhere((AppUser user2) => user2.Name == username2)
            .Merge("(user1)-[:IS_MANAGER_OF]->(user2)")
            .ExecuteWithoutResultsAsync();


            return Ok();
        }

        //Update a Single property on User
        [HttpPut("/UpdateSingleProperty")]
        public async Task<ActionResult> UpdateUser(string UserName, int age)
        {
            string username = UserName.ToLower();
            await _client.Cypher
            .Match("(user:User)")
            .Where((AppUser user) => user.Name == username)
            .Set("user.Age = $age")
            .WithParam("age", age)
            .ExecuteWithoutResultsAsync();

            return Ok();
        }

        //Replace all properties on a user
        [HttpPut("/UpdateEntireUser")]
        public async Task<ActionResult> UpdateEntireUser([FromBody] AppUser UpdatedUser)
        {
            AppUser UpdatedNewUser = new AppUser()
            {
                Id = UpdatedUser.Id,
                Name = UpdatedUser.Name.ToLower(),
                Age = UpdatedUser.Age,
                Email = UpdatedUser.Email
            };
            await _client.Cypher
            .Match("(user:User)")
            .Where((AppUser user) => user.Name == UpdatedNewUser.Name)
            .Set("user = $UpdatedNewUser")
            .WithParam("UpdatedNewUser", UpdatedNewUser)
            .ExecuteWithoutResultsAsync();

            return Ok();
        }

        // Delete a user and Relation
        [HttpDelete("/DeleteUser")]
        public async Task<ActionResult> DeleteUser(string UserName)
        {
            string username = UserName.ToLower();
            await _client.Cypher
            .OptionalMatch("(user:User)-[r]-()")
            .Where((AppUser user) => user.Name == username)
            .DetachDelete("user, r")
            .ExecuteWithoutResultsAsync();

            return Ok();
        }

        [HttpDelete("/DeleteUserNode")]
        public async Task<ActionResult> DeleteUserNode(string UserName)
        {
            string username = UserName.ToLower();
            await _client.Cypher
            .Match("(user:User)")
            .Where((AppUser user) => user.Name == username)
            .Delete("user")
            .ExecuteWithoutResultsAsync();

            return Ok();
        }

        // Delete all nodes and Relations
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            await _client.Cypher.Match("(n)")
            .DetachDelete("n")
            .ExecuteWithoutResultsAsync();

            return Ok();
        }

        //Get all labels for a specific user, and still the user too
        [HttpGet("/GetAllLabelsSpecificUser")]
        public async Task<ActionResult> GetAllLabelsUser(string UserName)
        {
            string username = UserName.ToLower();
            var user = await _client.Cypher
            .Match("(user:User)")
            .Where((AppUser user) => user.Name == username)
            .Return(user => new
            {
                User = user.As<AppUser>(),
                Labels = user.Labels()
            })
            .ResultsAsync;

            return Ok(user);
        }






    }
}