// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IQALanguageModelException.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LlmLib.Substrate
{
    /// <summary>
    /// Represents errors that occur within the IQA Language Model.
    /// </summary>
    [Serializable]
    public class IQALanguageModelException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IQALanguageModelException"/> class.
        /// </summary>
        public IQALanguageModelException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IQALanguageModelException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IQALanguageModelException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IQALanguageModelException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IQALanguageModelException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
