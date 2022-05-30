using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WordleHelperApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WordleController : ControllerBase
    {

        private IOptions<ApplicationConfiguration> _optionsApplicationConfiguration;
        public WordleController(IOptions<ApplicationConfiguration> o)
        {
            _optionsApplicationConfiguration = o;
#if LATER
            List<GuessLetter[]> guesses = new List<GuessLetter[]>();
            GuessLetter[] guess = new GuessLetter[5];
            guess[0] = new GuessLetter("P", GuessType.WrongLetter);
            guess[1] = new GuessLetter("T", GuessType.WrongLetter);
            guess[2] = new GuessLetter("U", GuessType.RightPosition);
            guess[3] = new GuessLetter("S", GuessType.WrongLetter);
            guess[4] = new GuessLetter("A", GuessType.WrongPosition);

            guesses.Add(guess);

            guess = new GuessLetter[5];
            guess[0] = new GuessLetter("L", GuessType.RightPosition);
            guess[1] = new GuessLetter("W", GuessType.WrongLetter);
            guess[2] = new GuessLetter("U", GuessType.WrongLetter);
            guess[3] = new GuessLetter("S", GuessType.WrongLetter);
            guess[4] = new GuessLetter("Z", GuessType.WrongLetter);

            guesses.Add(guess);
             GetSuggestions(new GetSuggestionsRequest(guesses));
#endif
        }

        // GET: api/<WordleController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }


        // GET api/<WordleController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return $"You asked for {id}";
        }

        // POST api/<WordleController>
        [HttpPost]
        public async Task<TestHttpResponse> PostAsync(TestHttpRequest request)
        {
            List<string> suggestions = new List<string>(); 
                var builder = new MySqlConnectionStringBuilder
                {
                    Server = $"{_optionsApplicationConfiguration.Value.Server}",
                    Database = "entries",
                    UserID = $"{_optionsApplicationConfiguration.Value.UserID}",
                    Password = $"{_optionsApplicationConfiguration.Value.Password}",
                    SslMode = MySqlSslMode.Required
                };

                using (var conn = new MySqlConnection(builder.ConnectionString))
                {
                    await conn.OpenAsync();

                    using (var command = conn.CreateCommand())
                    {
                    command.CommandText = "SELECT * FROM entries.scrabble WHERE word  like 'truc%';";
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suggestions.Add(reader["word"] as string);
                        }
                    }
#if LATER
                        command.CommandText = "DROP TABLE IF EXISTS inventory;";
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine("Finished dropping table (if existed)");

                        command.CommandText = "CREATE TABLE inventory (id serial PRIMARY KEY, name VARCHAR(50), quantity INTEGER);";
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine("Finished creating table");

                        command.CommandText = @"INSERT INTO inventory (name, quantity) VALUES (@name1, @quantity1),
                        (@name2, @quantity2), (@name3, @quantity3);";
                        command.Parameters.AddWithValue("@name1", "banana");
                        command.Parameters.AddWithValue("@quantity1", 150);
                        command.Parameters.AddWithValue("@name2", "orange");
                        command.Parameters.AddWithValue("@quantity2", 154);
                        command.Parameters.AddWithValue("@name3", "apple");
                        command.Parameters.AddWithValue("@quantity3", 100);

                        int rowCount = await command.ExecuteNonQueryAsync();
                        //Console.WriteLine(String.Format("Number of rows inserted={0}", rowCount));
#endif

                    }

                }

            
            return new TestHttpResponse(request, suggestions);
        }

        // POST api/<WordleController>
        [Route("GetSuggestions")]
        [HttpPost]

        //public async Task<GetSuggestionsResponse> GetSuggestions(GuessLetter[][] guesses)
        public async Task<GetSuggestionsResponse> GetSuggestions(GetSuggestionsRequest guesses)
        {
            List<string> suggestions = new List<string>();
            var builder = new MySqlConnectionStringBuilder
            {
                Server = $"{_optionsApplicationConfiguration.Value.Server}",
                Database = "entries",
                UserID = $"{_optionsApplicationConfiguration.Value.UserID}",
                Password = $"{_optionsApplicationConfiguration.Value.Password}",
                SslMode = MySqlSslMode.Required
            };

            string sqlQuery = BuildSqlQuery(guesses); // "SELECT * FROM entries.scrabble WHERE word  like 'lau%';";
            using (var conn = new MySqlConnection(builder.ConnectionString))
            {
                await conn.OpenAsync();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suggestions.Add(reader["word"] as string);
                        }
                    }

                }

            }

            return new GetSuggestionsResponse(null, suggestions, guesses.ReturnQuery ? sqlQuery : null);
            //return new GetSuggestionsResponse(request, suggestions);
        }

        enum LetterStatus
        {
            WrongCharacter, // gray
            WrongPosition, // yelow
            RightPosition // green
        }
        class KnownPosition : IEquatable<KnownPosition>
        {
            public int Position;
            public string Character;
            public KnownPosition(int position, string character)
            {
                this.Position = position;
                this.Character = character;
            }

            // IEquatable (so we can do list.Contains() check
            public bool Equals([AllowNull] KnownPosition other)
            {
                return this.Position == other.Position && this.Character.Equals(other.Character);
            }
        }

        string[] KnownPositions = new string[5]; // AND substring(word,5,1) = 'E' 
        List<string> KnownWrong = new List<string>(); // AND word not like '%G%' 
        List<string> KnownContains = new List<string>(); // AND word like '%D%' 
        List<KnownPosition> KnownWrongPositions = new List<KnownPosition>(); // AND substring(word,1,1) != 'L' 



        private string BuildSqlQuery(GetSuggestionsRequest guesses)
        {
            foreach( GuessLetter[] guess in guesses.Guesses)
            {
                for (int i = 0; i < guess.Length; i++)
                {
                    if (guess[i].type == GuessType.RightPosition)
                    {
                        KnownPositions[i] = guess[i].letter; 

                        // make sure it is not in the KnownWrong list (e.g. KNEED where 1st E is gray, 2nd E is green)
                        if (KnownWrong.Contains(guess[i].letter))
                        {
                            KnownWrong.Remove(guess[i].letter);
                        }
                    }
                    else if (guess[i].type == GuessType.WrongLetter)
                    {
                        // Add to list if not already in the KnownWrong or KnownContains lists
                        if (!KnownWrong.Contains(guess[i].letter) && !KnownContains.Contains(guess[i].letter))
                        {
                            KnownWrong.Add(guess[i].letter);
                        }
                    }
                    else
                    {
                        // Add to list if not already in list
                        if (!KnownContains.Contains(guess[i].letter))
                        {
                            KnownContains.Add(guess[i].letter);
                        }
                        KnownPosition knownWrongPosition = new KnownPosition(i, guess[i].letter);
                        if (!KnownWrongPositions.Contains(knownWrongPosition))
                        {
                            KnownWrongPositions.Add(new KnownPosition(i, guess[i].letter));
                        }

                        // make sure it is not in the KnownWrong list (e.g. KNEED where 1st E is gray, 2nd E is yellow)
                        if (KnownWrong.Contains(guess[i].letter))
                        {
                            KnownWrong.Remove(guess[i].letter);
                        }
                    }
                }
            }

            return BuildSqlQuery();
        }

        private string BuildSqlQuery()
        {
            const string table = "entries.scrabble";
            StringBuilder sb = new StringBuilder($"SELECT * FROM {table} WHERE CHAR_LENGTH(word) = 5{Environment.NewLine}");

            // has these characters in these positions
            for (int i = 0; i < KnownPositions.Length; i++)
            {
                if (KnownPositions[i] != null)
                {
                    sb.AppendLine($"AND substring(word, {i + 1}, 1) = '{KnownPositions[i]}'");
                }
            }

            // doesn’t contain these letters
            for (int i = 0; i < KnownWrong.Count; i++)
            {
                sb.AppendLine($"AND word not like '%{KnownWrong[i]}%'");
            }

            // contains these letters(don’t include known positions)
            foreach (string s in KnownContains)
            {
                sb.AppendLine($"AND word like '%{s}%'");
            }

            // these chars not in these positions 
            foreach (KnownPosition k in KnownWrongPositions)
            {
                sb.AppendLine($"AND substring(word,{k.Position + 1},1) != '{k.Character}'");
            }

            sb.AppendLine("GROUP BY word LIMIT 0, 100");
            string sqlQuery = sb.ToString();
            
            return sqlQuery;
        }


        // PUT api/<WordleController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<WordleController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    #region Global Types

    public enum GuessType
    {
        RightPosition = 1,
        WrongLetter = 2,
        WrongPosition = 3
    }

    [DataContract]
    public class GuessLetter
    {
        [DataMember(EmitDefaultValue = false)]
        public GuessType type { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string letter { get; set; }

        public GuessLetter(string letter, GuessType type)
        {
            this.letter = letter;
            this.type = type;
        }
    }

    [DataContract]
    public class GetSuggestionsRequest
    {
        #region Properties

        /// <summary>
        /// Guesses
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GuessLetter[][] Guesses { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool ReturnQuery { get; set; }

        #endregion

        #region Constructors

        public GetSuggestionsRequest()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringValue"></param>
        /// <param name="intValue"></param>
        public GetSuggestionsRequest(List<GuessLetter[]> guesses, bool returnQuery)
            : this(guesses.ToArray(), returnQuery)
        {
        }

        public GetSuggestionsRequest(GuessLetter[][] guesses, bool returnQuery)
        {
            this.Guesses = guesses;
            this.ReturnQuery = returnQuery;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"GetSuggestionsRequest: {this.Guesses.Length} guesses";
        }

        #endregion

        #region Types

        #endregion
    }

    [DataContract]
    public class GetSuggestionsResponse : ErrorResponse
    {
        #region Properties

        /// <summary>
        /// String array of words that match request criteria
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string[] Suggestions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string SqlQuery { get; set; }

        #endregion

        #region Constructors

        public GetSuggestionsResponse()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iUserInterface"></param>
        /// <param name="request"></param>
        public GetSuggestionsResponse(GetSuggestionsRequest request, List<string> suggestions, string sqlQuery)
        {
            if (suggestions != null)
            {
                this.Suggestions = suggestions.ToArray();
            }

            this.SqlQuery = sqlQuery;
        }

        public GetSuggestionsResponse(string[] suggestions, int errorCode, string errorDescription, LTApiError ltApiError)
            : base(errorCode, errorDescription, ltApiError)
        {
            this.Suggestions = suggestions;
        }


        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"GetSuggestionsResponse: {this.Suggestions?.Length} suggestions";
        }



        #endregion

        #region Types

        #endregion
    }

    /// <summary>
    /// Test request
    /// </summary>
    [DataContract]
    public class TestHttpRequest
    {
        #region Properties

        /// <summary>
        /// Test string value
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string StringValue { get; set; }

        /// <summary>
        /// Test integer value
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? IntValue { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringValue"></param>
        /// <param name="intValue"></param>
        public TestHttpRequest(string stringValue, int? intValue)
        {
            this.StringValue = stringValue;
            this.IntValue = intValue;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "TestHttpRequest: StringValue({0}) IntValue({1})", this.StringValue, this.IntValue);
        }

        #endregion

        #region Types

        #endregion
    }


    /// <summary>
    /// Response to test   reequest
    /// </summary>
    [DataContract]
    public class TestHttpResponse : ErrorResponse
    {
        #region Properties

        /// <summary>
        /// String value in test request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string[] Suggestions { get; set; }

        /// <summary>
        /// String value in test request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string RequestStringValue { get; set; }

        /// <summary>
        /// Integer value in test request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? RequestIntValue { get; set; }

        /// <summary>
        /// Response string (backwards version of request string)
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ResponseStringValue { get; set; }

        /// <summary>
        /// Response integer (manipulated version of request integer)
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ResponseIntValue { get; set; }

        bool IsValid
        {
            get
            {
                bool isValid = false;
                try
                {
                    isValid = this.ResponseStringValue.Equals(Encode(this.RequestStringValue))
                        && (this.ResponseIntValue.Value == Encode(this.RequestIntValue));
                }
                catch { }

                return isValid;
            }
        }

        #endregion

        #region Constructors

        public TestHttpResponse()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iUserInterface"></param>
        /// <param name="request"></param>
        public TestHttpResponse(TestHttpRequest request, List<string> suggestions)
        {
            this.RequestStringValue = request.StringValue;
            this.RequestIntValue = request.IntValue;

            this.ResponseStringValue = Encode(request.StringValue);
            this.ResponseIntValue = Encode(request.IntValue);

            this.Suggestions = suggestions.ToArray();
        }

        public TestHttpResponse(string[] suggestions, string requestStringValue, int? requestIntValue, string responseStringValue, int? responseIntValue, int errorCode, string errorDescription, LTApiError ltApiError)
            : base(errorCode, errorDescription, ltApiError)
        {
            this.Suggestions = suggestions;
            this.RequestStringValue = requestStringValue;
            this.RequestIntValue = requestIntValue;
            this.ResponseStringValue = responseStringValue;
            this.ResponseIntValue = responseIntValue;
        }


        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "TestHttpResponse: IsValid({0}) RequestStringValue({1}) RequestIntValue({2}) ResponseStringValue({3}) ResponseIntValue({4})", this.IsValid, this.RequestStringValue, this.RequestIntValue, this.ResponseStringValue, this.ResponseIntValue);
        }
        string Encode(string s)
        {
            return new string(s.Reverse().ToArray());
        }

        int Encode(int? i)
        {
            if (!i.HasValue)
            {
                i = 10;
            }

            return (i.Value + 3) * 9;
        }



        #endregion

        #region Types

        #endregion
    }


    /// <summary>
    /// Error portion of API response
    /// </summary>
    [DataContract]
    public class ErrorResponse
    {

        #region Fields

        /// <summary>
        /// 
        /// </summary>
        public int errorCode = (int)LTApiError.Success;
        /// <summary>
        /// 
        /// </summary>
        public string errorDescription = null;

        #endregion

        #region Properties

        /// <summary>
        /// Error code
        /// </summary>

        [DataMember]
        public int ErrorCode { get { return this.errorCode; } set { this.errorCode = value; } }

        /// <summary>
        /// Error details for the developer (English)
        /// </summary>
        [DataMember]
        public string ErrorDescription { get { return this.errorDescription; } set { this.errorDescription = value; } }


        /// <summary>
        /// 
        /// </summary>
        public LTApiError ltApiError { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public ErrorResponse()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iUserInterface"></param>
        /// <param name="error"></param>
        /// <param name="message"></param>
        public ErrorResponse(LTApiError error, string message)
            : this(errorCode: (int)error, errorDescription: message, ltApiError: error)
        {
        }

        public ErrorResponse(int errorCode, string errorDescription, LTApiError ltApiError)
        {
            this.ErrorCode = errorCode;
            this.ErrorDescription = errorDescription;
            this.ltApiError = ltApiError;


            // Note: this.ErrorDescription is always in english
            // If this.LocalizedErrorMessage is also english, null it out to reduce packet size
            if (!string.IsNullOrWhiteSpace(this.ErrorDescription))
            {
                this.ErrorDescription = null;
            }
        }



        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Error {0}: {1}", this.ErrorCode, this.ErrorDescription);
        }

        #endregion
    }


    public enum LTApiError
    {
        Unknown = -1,
        Success = 0,
        InternalError = 100,
        InvalidApiKey = 1001,
        TrackerIdNotFound = 1004,
        MissingApiKey = 1008,
        NoMatchingResults = 1009,
        InvalidParameter = 1010,
        MissingUserName = 2000,
        MissingPassword = 2001,
        InvalidCredentials = 2002,
        MissingAccessToken = 2003,
        InvalidRequestTimestamp = 2004,
        InvalidAccessToken = 2005,
        AccessDenied = 2006,
        UnauthenticatedAccessDenied = 2007,
        DifferentCustomers = 2008,
        IsLockedOut = 2009,
        TrackerAlreadyAssignedToTrip = 2010,
        CannotSpecifyEndLocationWhenSettingEndLocationUndefinedNoDestinationRequired = 2011,
        CustomerDatabaseNotAvailable = 2012,
        NoBreadcrumbsAvailable = 2013,
        DuplicateTripName = 2014,
        InvalidPassword = 20009
    }

    #endregion
}
