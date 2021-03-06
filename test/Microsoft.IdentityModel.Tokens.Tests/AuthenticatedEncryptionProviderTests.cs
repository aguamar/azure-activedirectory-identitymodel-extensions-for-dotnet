//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tests;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Tokens.Tests
{
    public class AuthenticatedEncryptionTestParams
    {
        public byte[] AuthenticatedData { get; set; }
        public byte[] Bytes { get; set; }
        public string DecryptAlgorithm { get; set; }
        public SecurityKey DecryptKey { get; set; }
        public ExpectedException EE { get; set; }
        public string EncryptAlgorithm { get; set; }
        public AuthenticatedEncryptionResult EncryptionResults { get; set; }
        public SecurityKey EncryptKey { get; set; }
        public bool IsSupportedAlgorithm { get; set; }
        public byte[] KeyBytes { get; set; }
        public int KeySize { get; set; }
        public byte[] Plaintext { get; set; }
        public AuthenticatedEncryptionProvider Provider { get; set; }
        public string TestId { get; set; }

        public override string ToString()
        {
            return $"{TestId}, {DecryptAlgorithm}, {EncryptAlgorithm}";
        }
    }

    /// <summary>
    /// Tests for AuthenticatedEncryptionProvider
    /// Constructors
    ///     - validate parameters (null, empty)
    ///     - algorithms supported
    ///     - key size
    ///     - properties are set correctly (Algorithm, Context, Key)
    /// EncryptDecrypt
    ///     - positive tests for keys (256, 384, 512, 768, 1024) X Algorithms supported.
    ///     - parameter validation for Encrypt
    /// Decrypt
    ///     - negative tests for tampering of (ciphertest, iv, authenticationtag, authenticateddata)
    ///     - parameter validataion for Decrypt
    /// DecryptMismatch
    ///     - negative tests for switching (keys, algorithms)
    /// EncryptVirtual
    ///     - tests virtual method was called
    /// DecryptVirtual
    ///     - tests virtual method was called
    /// </summary>
    public class AuthenticatedEncryptionProviderTests
    {
        [Theory, MemberData(nameof(AEPConstructorTheoryData))]
        public void Constructors(string testId, SymmetricSecurityKey key, string algorithm, ExpectedException ee)
        {
            TestUtilities.WriteHeader("Constructors - " + testId, true);
            try
            {
                var context = Guid.NewGuid().ToString();
                var provider = new AuthenticatedEncryptionProvider(key, algorithm) { Context = context };

                ee.ProcessNoException();

                Assert.Equal(provider.Algorithm, algorithm);
                Assert.Equal(provider.Context, context);
                Assert.True(ReferenceEquals(provider.Key, key));
            }
            catch (Exception ex)
            {
                ee.ProcessException(ex);
            }
        }

        public static TheoryData<string, SecurityKey, string, ExpectedException> AEPConstructorTheoryData()
        {
            var theoryData = new TheoryData<string, SecurityKey, string, ExpectedException>();

            theoryData.Add("Test1", null, null, ExpectedException.ArgumentNullException());
            theoryData.Add("Test2", Default.SymmetricEncryptionKey256, null, ExpectedException.ArgumentNullException());
            theoryData.Add("Test3", Default.SymmetricEncryptionKey256, SecurityAlgorithms.Aes128CbcHmacSha256, ExpectedException.NoExceptionExpected);
            theoryData.Add("Test4", Default.SymmetricEncryptionKey512, SecurityAlgorithms.Aes128CbcHmacSha256, ExpectedException.NoExceptionExpected);
            theoryData.Add("Test5", Default.SymmetricEncryptionKey512, SecurityAlgorithms.Aes256CbcHmacSha512, ExpectedException.NoExceptionExpected);
            theoryData.Add("Test6", Default.SymmetricEncryptionKey256, SecurityAlgorithms.Aes128Encryption, ExpectedException.ArgumentException("IDX10668:"));
            theoryData.Add("Test7", Default.SymmetricEncryptionKey128, SecurityAlgorithms.Aes128CbcHmacSha256, ExpectedException.ArgumentOutOfRangeException("IDX10653:"));
            theoryData.Add("Test8", Default.SymmetricEncryptionKey256, SecurityAlgorithms.Aes256CbcHmacSha512, ExpectedException.ArgumentOutOfRangeException("IDX10653:"));

            // set key.CryptoProviderFactory that return null when creating SignatureProvider
            var key = Default.SymmetricEncryptionKey256;
            key.CryptoProviderFactory = new DerivedCryptoProviderFactory
            {
                SymmetricSignatureProviderForSigning = null,
                SymmetricSignatureProviderForVerifying = null
            };
            theoryData.Add("Test9", key, SecurityAlgorithms.Aes128CbcHmacSha256, ExpectedException.ArgumentException("IDX10649:"));

            return theoryData;
        }

        [Theory, MemberData(nameof(DecryptTheoryData))]
        public void Decrypt(AuthenticatedEncryptionTestParams theoryParams)
        {
            try
            {
                theoryParams.Provider.Decrypt(theoryParams.EncryptionResults.Ciphertext, theoryParams.AuthenticatedData, theoryParams.EncryptionResults.IV, theoryParams.EncryptionResults.AuthenticationTag);
                theoryParams.EE.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryParams.EE.ProcessException(ex);
            }
        }

        public static TheoryData<AuthenticatedEncryptionTestParams> DecryptTheoryData()
        {
            var theoryData = new TheoryData<AuthenticatedEncryptionTestParams>();

            // tampering: AuthenticatedData, AuthenticationTag, Ciphertext, InitializationVector
            AddDecryptTamperedTheoryData("Test1", Default.SymmetricEncryptionKey256, SecurityAlgorithms.Aes128CbcHmacSha256, theoryData);
            AddDecryptTamperedTheoryData("Test2", Default.SymmetricEncryptionKey384, SecurityAlgorithms.Aes128CbcHmacSha256, theoryData);
            AddDecryptTamperedTheoryData("Test3", Default.SymmetricEncryptionKey512, SecurityAlgorithms.Aes128CbcHmacSha256, theoryData);
            AddDecryptTamperedTheoryData("Test4", Default.SymmetricEncryptionKey768, SecurityAlgorithms.Aes128CbcHmacSha256, theoryData);
            AddDecryptTamperedTheoryData("Test5", Default.SymmetricEncryptionKey1024, SecurityAlgorithms.Aes128CbcHmacSha256, theoryData);
            AddDecryptTamperedTheoryData("Test6", Default.SymmetricEncryptionKey512, SecurityAlgorithms.Aes256CbcHmacSha512, theoryData);
            AddDecryptTamperedTheoryData("Test7", Default.SymmetricEncryptionKey768, SecurityAlgorithms.Aes256CbcHmacSha512, theoryData);
            AddDecryptTamperedTheoryData("Test8", Default.SymmetricEncryptionKey1024, SecurityAlgorithms.Aes256CbcHmacSha512, theoryData);

            // parameter check: AuthenticatedData, AuthenticationTag, Ciphertext, InitializationVector - null / size 0
            AddDecryptParameterCheckTheoryData("Test9", null, new byte[1], new byte[1], new byte[1], theoryData);
            AddDecryptParameterCheckTheoryData("Test10", new byte[0], new byte[1], new byte[1], new byte[1], theoryData);
            AddDecryptParameterCheckTheoryData("Test11", new byte[1], null, new byte[1], new byte[1], theoryData);
            AddDecryptParameterCheckTheoryData("Test12", new byte[1], new byte[0], new byte[1], new byte[1], theoryData);
            AddDecryptParameterCheckTheoryData("Test13", new byte[1], new byte[1], null, new byte[1], theoryData);
            AddDecryptParameterCheckTheoryData("Test14", new byte[1], new byte[1], new byte[0], new byte[1], theoryData);
            AddDecryptParameterCheckTheoryData("Test15", new byte[1], new byte[1], new byte[1], null, theoryData);
            AddDecryptParameterCheckTheoryData("Test16", new byte[1], new byte[1], new byte[1], new byte[0], theoryData);

            return theoryData;
        }

        private static void AddDecryptTamperedTheoryData(string testId, SymmetricSecurityKey key, string algorithm, TheoryData<AuthenticatedEncryptionTestParams> theoryData)
        {
            var authenticatedData = Guid.NewGuid().ToByteArray();
            var plainText = Guid.NewGuid().ToByteArray();
            var provider = new AuthenticatedEncryptionProvider(key, algorithm);
            var results = provider.Encrypt(plainText, authenticatedData);

            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = Guid.NewGuid().ToByteArray(),
                DecryptAlgorithm = algorithm,
                DecryptKey = key,
                EE = ExpectedException.SecurityTokenDecryptionFailedException("IDX10650:"),
                EncryptAlgorithm = algorithm,
                EncryptKey = key,
                EncryptionResults = results,
                Provider = provider,
                TestId = "AddDecryptTheoryData1_" + testId
            });

            results = provider.Encrypt(plainText, authenticatedData);
            TestUtilities.XORBytes(results.IV);
            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = authenticatedData,
                DecryptAlgorithm = algorithm,
                DecryptKey = key,
                EE = ExpectedException.SecurityTokenDecryptionFailedException("IDX10650:"),
                EncryptAlgorithm = algorithm,
                EncryptKey = key,
                EncryptionResults = results,
                Provider = provider,
                TestId = "AddDecryptTheoryData2_" + testId
            });

            results = provider.Encrypt(plainText, authenticatedData);
            TestUtilities.XORBytes(results.AuthenticationTag);
            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = authenticatedData,
                DecryptAlgorithm = algorithm,
                DecryptKey = key,
                EE = ExpectedException.SecurityTokenDecryptionFailedException("IDX10650:"),
                EncryptAlgorithm = algorithm,
                EncryptKey = key,
                EncryptionResults = results,
                Provider = provider,
                TestId = "AddDecryptTheoryData3_" + testId
            });

            results = provider.Encrypt(plainText, authenticatedData);
            TestUtilities.XORBytes(results.Ciphertext);
            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = authenticatedData,
                DecryptAlgorithm = algorithm,
                DecryptKey = key,
                EE = ExpectedException.SecurityTokenDecryptionFailedException("IDX10650:"),
                EncryptAlgorithm = algorithm,
                EncryptKey = key,
                EncryptionResults = results,
                Provider = provider,
                TestId = "AddDecryptTheoryData4_" + testId
            });
        }

        private static void AddDecryptParameterCheckTheoryData(string testId, byte[] authenticatedData, byte[] authenticationTag, byte[] cipherText, byte[] iv, TheoryData<AuthenticatedEncryptionTestParams> theoryData)
        {
            var provider = new AuthenticatedEncryptionProvider(Default.SymmetricEncryptionKey256, SecurityAlgorithms.Aes128CbcHmacSha256);
            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = authenticatedData,
                EE = ExpectedException.ArgumentNullException(),
                EncryptionResults = new AuthenticatedEncryptionResult(Default.SymmetricEncryptionKey256, cipherText, iv, authenticationTag),
                Provider = provider,
                TestId = testId
            });
        }

        [Theory, MemberData(nameof(DecryptMismatchTheoryData))]
        public void DecryptMismatch(AuthenticatedEncryptionTestParams theoryParams)
        {
            try
            {
                theoryParams.Provider.Decrypt(theoryParams.EncryptionResults.Ciphertext, theoryParams.AuthenticatedData, theoryParams.EncryptionResults.IV, theoryParams.EncryptionResults.AuthenticationTag);
                theoryParams.EE.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryParams.EE.ProcessException(ex);
            }
        }
        public static TheoryData<AuthenticatedEncryptionTestParams> DecryptMismatchTheoryData()
        {
            var theoryData = new TheoryData<AuthenticatedEncryptionTestParams>();
            var keys128 = new List<SymmetricSecurityKey> { Default.SymmetricEncryptionKey256, Default.SymmetricEncryptionKey384, Default.SymmetricEncryptionKey512, Default.SymmetricEncryptionKey768, Default.SymmetricEncryptionKey1024 };
            var keys256 = new List<SymmetricSecurityKey> { Default.SymmetricEncryptionKey512, Default.SymmetricEncryptionKey768, Default.SymmetricEncryptionKey1024 };
            var keys128_256 = new List<SymmetricSecurityKey> { Default.SymmetricEncryptionKey512, Default.SymmetricEncryptionKey768, Default.SymmetricEncryptionKey1024, Default.SymmetricEncryptionKey256, Default.SymmetricEncryptionKey384 };

            for (int i = 0; i < keys128.Count - 1; i++)
                for(int j = i + 1; j < keys128.Count; j++)
                    AddDecryptMismatchTheoryData(
                        "Test1-" + i.ToString() + "-" + j.ToString(),
                        keys128[i],
                        keys128[j],
                        SecurityAlgorithms.Aes128CbcHmacSha256,
                        SecurityAlgorithms.Aes128CbcHmacSha256,
                        ExpectedException.SecurityTokenDecryptionFailedException(),
                        theoryData);

            for (int i = keys128.Count - 1; i > 0; i--)
                for (int j = i - 1; j > -1; j--)
                    AddDecryptMismatchTheoryData(
                        "Test2-" + i.ToString() + "-" + j.ToString(),
                        keys128[i],
                        keys128[j],
                        SecurityAlgorithms.Aes128CbcHmacSha256,
                        SecurityAlgorithms.Aes128CbcHmacSha256,
                        ExpectedException.SecurityTokenDecryptionFailedException(),
                        theoryData);

            for (int i = 0; i < keys256.Count - 1; i++)
                for (int j = i + 1; j < keys256.Count; j++)
                    AddDecryptMismatchTheoryData(
                        "Test3-" + i.ToString() + "-" + j.ToString(),
                        keys256[i],
                        keys256[j],
                        SecurityAlgorithms.Aes256CbcHmacSha512,
                        SecurityAlgorithms.Aes256CbcHmacSha512,
                        ExpectedException.SecurityTokenDecryptionFailedException(),
                        theoryData);

            for (int i = keys256.Count - 1; i > 0; i--)
                for (int j = i - 1; j > -1; j--)
                    AddDecryptMismatchTheoryData(
                        "Test4-" + i.ToString() + "-" + j.ToString(),
                        keys256[i],
                        keys256[j],
                        SecurityAlgorithms.Aes256CbcHmacSha512,
                        SecurityAlgorithms.Aes256CbcHmacSha512,
                        ExpectedException.SecurityTokenDecryptionFailedException(),
                        theoryData);

            for (int i = 0; i < keys256.Count - 1; i++)
                for (int j = 0; j < keys128.Count; j++)
                    AddDecryptMismatchTheoryData(
                        "Test5-" + i.ToString() + "-" + j.ToString(),
                        keys128[j],
                        keys256[i],
                        SecurityAlgorithms.Aes128CbcHmacSha256,
                        SecurityAlgorithms.Aes256CbcHmacSha512,
                        ExpectedException.SecurityTokenDecryptionFailedException(),
                        theoryData);

            return theoryData;
        }

        private static void AddDecryptMismatchTheoryData(
            string testId,
            SymmetricSecurityKey decryptKey,
            SymmetricSecurityKey encryptkey,
            string decryptAlgorithm,
            string encryptAlgorithm,
            ExpectedException ee,
            TheoryData<AuthenticatedEncryptionTestParams> theoryData)
        {
            var authenticatedData = Guid.NewGuid().ToByteArray();
            var plainText = Guid.NewGuid().ToByteArray();
            var provider = new AuthenticatedEncryptionProvider(encryptkey, encryptAlgorithm);
            var results = provider.Encrypt(plainText, authenticatedData);
            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = authenticatedData,
                DecryptAlgorithm = decryptAlgorithm,
                DecryptKey = decryptKey,
                EE = ee,
                EncryptionResults = results,
                Provider = new AuthenticatedEncryptionProvider(decryptKey, decryptAlgorithm),
                TestId = testId
            });
        }

        [Theory, MemberData(nameof(EncryptDecryptTheoryData))]
        public void EncryptDecrypt(AuthenticatedEncryptionTestParams theoryParams)
        {
            try
            {
                // use a different provider for encrypting and decrypting to ensure key creation / privated vars are set correctly
                var encryptionProvider = new AuthenticatedEncryptionProvider(theoryParams.EncryptKey, theoryParams.DecryptAlgorithm);
                var decryptionProvider = new AuthenticatedEncryptionProvider(theoryParams.DecryptKey, theoryParams.EncryptAlgorithm);
                var results = encryptionProvider.Encrypt(theoryParams.Plaintext, theoryParams.AuthenticatedData);
                var cleartext = decryptionProvider.Decrypt(results.Ciphertext, theoryParams.AuthenticatedData, results.IV, results.AuthenticationTag);

                Assert.True(Utility.AreEqual(theoryParams.Plaintext, cleartext), "theoryParams.PlainText != clearText");

                theoryParams.EE.ProcessNoException();
            }
            catch (Exception ex)
            {
                theoryParams.EE.ProcessException(ex);
            }
        }

        public static TheoryData<AuthenticatedEncryptionTestParams> EncryptDecryptTheoryData()
        {
            var theoryData = new TheoryData<AuthenticatedEncryptionTestParams>();

            // round trip positive tests
            AddEncryptDecryptTheoryData("Test1", SecurityAlgorithms.Aes128CbcHmacSha256, Default.SymmetricEncryptionKey256, theoryData);
            AddEncryptDecryptTheoryData("Test2", SecurityAlgorithms.Aes128CbcHmacSha256, Default.SymmetricEncryptionKey384, theoryData);
            AddEncryptDecryptTheoryData("Test3", SecurityAlgorithms.Aes128CbcHmacSha256, Default.SymmetricEncryptionKey512, theoryData);
            AddEncryptDecryptTheoryData("Test4", SecurityAlgorithms.Aes128CbcHmacSha256, Default.SymmetricEncryptionKey768, theoryData);
            AddEncryptDecryptTheoryData("Test5", SecurityAlgorithms.Aes128CbcHmacSha256, Default.SymmetricEncryptionKey1024, theoryData);
            AddEncryptDecryptTheoryData("Test6", SecurityAlgorithms.Aes256CbcHmacSha512, Default.SymmetricEncryptionKey512, theoryData);
            AddEncryptDecryptTheoryData("Test7", SecurityAlgorithms.Aes256CbcHmacSha512, Default.SymmetricEncryptionKey768, theoryData);
            AddEncryptDecryptTheoryData("Test8", SecurityAlgorithms.Aes256CbcHmacSha512, Default.SymmetricEncryptionKey1024, theoryData);

            // Encrypt parameter checking
            AddEncryptParameterCheckTheoryData("Test9",  null,        new byte[1], theoryData);
            AddEncryptParameterCheckTheoryData("Test10", new byte[0], new byte[1], theoryData);
            AddEncryptParameterCheckTheoryData("Test11", new byte[1], null,        theoryData);
            AddEncryptParameterCheckTheoryData("Test12", new byte[1], new byte[0], theoryData);

            return theoryData;
        }

        private static void AddEncryptDecryptTheoryData(string testId, string algorithm, SymmetricSecurityKey key, TheoryData<AuthenticatedEncryptionTestParams> theoryData)
        {
            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = Guid.NewGuid().ToByteArray(),
                DecryptAlgorithm = algorithm,
                DecryptKey = key,
                EE = ExpectedException.NoExceptionExpected,
                EncryptAlgorithm = algorithm,
                EncryptKey = key,
                Plaintext = Guid.NewGuid().ToByteArray(),
                TestId = "AddEncryptDecryptTheoryData_" + testId
            });
        }

        private static void AddEncryptParameterCheckTheoryData(string testId, byte[] authenticatedData, byte[] plainText, TheoryData<AuthenticatedEncryptionTestParams> theoryData)
        {
            var provider = new AuthenticatedEncryptionProvider(Default.SymmetricEncryptionKey256, SecurityAlgorithms.Aes128CbcHmacSha256);
            theoryData.Add(new AuthenticatedEncryptionTestParams
            {
                AuthenticatedData = authenticatedData,
                EE = ExpectedException.ArgumentNullException(),
                EncryptionResults = new AuthenticatedEncryptionResult(Default.SymmetricEncryptionKey256, new byte[1], new byte[1], new byte[1]),
                Plaintext = plainText,
                Provider = provider,
                TestId = testId
            });
        }
    }
}

#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
