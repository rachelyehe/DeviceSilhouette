﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateManagementServiceWebAPI.Models
{
    /// <summary>
    /// Representation for errors in API responses
    /// </summary>
    public class ErrorModel
    {
        /// <summary>
        /// The identifier for the error
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Non-localized error description
        /// </summary>
        public string Message { get; set; }

        // TODO - need to implement logging and include tracking id in error message
        //public string TrackingId { get; set; }
    }




    /// <summary>
    /// Error model for ModelState validation errors
    /// </summary>
    public class InvalidRequestErrorModel : ErrorModel
    {
        /// <summary>
        /// The set of ModelState validation failures
        /// </summary>
        public List<ValidationMessage> ValidationMessages { get; set; }
    }
    /// <summary>
    /// Representation of a ModelState validation failure
    /// </summary>
    public class ValidationMessage
    {
        /// <summary>
        /// The name of the request property that failed validation
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// The ModelState validation failure messages for the property 
        /// </summary>
        public List<string> Messages { get; set; }
    }




    /// <summary>
    /// Representation for unhandled errors
    /// </summary>
    public class UnhandledErrorModel : ErrorModel
    {
        /// <summary>
        /// Provide details on the cause of the error
        /// </summary>
        public InnerErrorModel InnerError { get; set; }
    }
    /// <summary>
    /// Representation of the error info for unhandled errors
    /// </summary>
    public class InnerErrorModel
    {
        /// <summary>
        /// The message for an unhandled error
        /// </summary>
        public string Message { get; set; }
    }




    /// <summary>
    /// List of error codes
    /// </summary>
    public static class ErrorCode // TODO - revisit in relation to https://github.com/Microsoft/api-guidelines/blob/master/Guidelines.md#7102-error-condition-responses
    {
        /// <summary>
        /// The specified device id wasn't found
        /// </summary>
        public const string EntityNotFound = "invalid-identifier";
        /// <summary>
        /// The request body/parameters are not valid
        /// </summary>
        public const string InvalidRequest = "invalid-request";


        /// <summary>
        /// Generic unhandled error - this should be the LAST RESORT of error handling!!
        /// </summary>
        public const string UnhandledError = "unhandled-error";
    }

    /// <summary>
    /// List of error messages
    /// </summary>
    public static class ErrorMessage
    {
        /// <summary>
        /// The specified device id wasn't found
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static string EntityNotFound_DeviceMessage(string deviceId, int version)
        {
            return $"Message '{version}' not found for device '{deviceId}'";
        }

        internal static string EntityNotFound_DeviceCommand(string deviceId, string commandId)
        {
            return $"Command'{commandId}' not found for device '{deviceId}'";
        }
        /// <summary>
        /// The request body/parameters are not valid
        /// </summary>>
        /// <returns></returns>
        public static string InvalidRequest()
        {
            return "The request body was invalid";
        }

        /// <summary>
        /// Generic unhandled error - this should be the LAST RESORT of error handling!!
        /// </summary>
        public static string UnhandledError()
        {
            return "Unhandled error";
        }

    }


}

