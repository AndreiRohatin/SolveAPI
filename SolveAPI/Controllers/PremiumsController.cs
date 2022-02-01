#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SolveAPI.Models;

namespace SolveAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PremiumsController : BaseController
    {
        IFirebaseClient _client;

        public PremiumsController()
        {
            try
            {
                IFirebaseConfig config = new FirebaseConfig
                {
                    AuthSecret = "",
                    BasePath = ""
                };
                _client = new FirebaseClient(config);
            }
            catch
            {
                //connection failed
            }
        }

        // GET: api/Premiums
        [HttpGet]
        public async Task<IEnumerable<Premium>> GetPremiums()
        {
            FirebaseResponse rspv = await _client.GetAsync("path/to/be/set");
            return rspv.ResultAs<IEnumerable<Premium>>();
        }

        // GET: api/Premiums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Premium>> GetPremium(string id)
        {
            FirebaseResponse rspv = await _client.GetAsync("path/to/be/set");
            return rspv.ResultAs<Premium>();
        }

        // PUT: api/Premiums/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPremium(string id, Premium premium)
        {
            if (id != premium.UID.ToString())
            {
                return BadRequest();
            }

            //TODO
            //create logic for update
            FirebaseResponse response = await _client.UpdateAsync($"todos/set/{id}", premium);
            Premium todo = response.ResultAs<Premium>();
            return NoContent();
        }

        // POST: api/Premiums
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Premium>> PostPremium(Premium premium)
        {
            PushResponse response = await _client.PushAsync("todos/push", premium);
            return CreatedAtAction("GetPremium", new { id = premium.UID }, premium);
        }

        // DELETE: api/Premiums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePremium(string id)
        {
            FirebaseResponse response = await _client.DeleteAsync("todos");
            Console.WriteLine(response.StatusCode);
            return NoContent();
        }

    }
}
