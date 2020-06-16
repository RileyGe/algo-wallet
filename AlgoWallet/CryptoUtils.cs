﻿using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Security;
using System.Text;
using DynamicData;
using System.Linq;

namespace AlgoWallet
{
    public class CryptoUtils
    {
        public static byte[] EncryptAesGcm(byte[] key, byte[] nonce, byte[] plaintext)
        {
            byte[] tag = new byte[16];
            //var nonce = Encoding.UTF8.GetBytes("algo--wallet");
            byte[] cipherText = new byte[plaintext.Length];
            using var cipher = new AesGcm(key);
            cipher.Encrypt(nonce, plaintext, cipherText, tag);
            var cipherList = cipherText.ToList();
            cipherList.Add(tag);
            return cipherList.ToArray();
        }
        public static byte[] GetCipherTextFromAesGcmResult(byte[] result)
        {
            if (result.Length == 16 + 32)
                return result.AsSpan().Slice(0, 32).ToArray();            
            else
                return null;
        }
        public static byte[] GetTagFromAesGcmResult(byte[] result)
        {
            if (result.Length == 16 + 32)
                return result.AsSpan().Slice(32, 16).ToArray();
            else
                return null;
        }
        public static byte[] DecryptAesGcm(byte[] key, byte[] nonce, byte[] cipherText, byte[] tag)
        {
            byte[] decryptedData = new byte[cipherText.Length];
            using var cipher = new AesGcm(key);
            cipher.Decrypt(nonce, cipherText, tag, decryptedData);
            return decryptedData;
        }
        /// <summary>
        /// Generate a random 16 bits salt
        /// </summary>
        /// <returns>16 bits salt</returns>
        public static byte[] GenerateRandomSalt()
        {
            var salt = new byte[16];
            new SecureRandom().NextBytes(salt);
            //new Random().NextBytes(salt);
            return salt;
        }
        public static byte[] GenerateHash(byte[] salt, string pwd)
        {
            if (salt.Length != 16)
                return null;

            var password = Encoding.UTF8.GetBytes(pwd);
            const int SCryptN = 262144;
            // Actual SCrypt computation
            // r=8, p=1 are default parameters
            // dkLen=24 = output a 24-byte key
            return SCrypt.Generate(password, salt, SCryptN, 8, 1, 48);
        }
        public static byte[] GetCheckSalt(byte[] key)
        {
            if (key.Length != 48)
                return null;
            var key_list = new List<byte>(key);
            key_list.RemoveRange(16, 32);
            return key_list.ToArray();
        }
        public static byte[] GetMasterKey(byte[] key)
        {
            if (key.Length != 48)
                return null;
            var key_list = new List<byte>(key);
            key_list.RemoveRange(0, 16);
            return key_list.ToArray();
        }
        /// <summary>
        /// The password is hashed with BCrypt using the format in OpenBSD 
        /// with a cost factor of 12 and a random salt
        /// </summary>
        /// <param name="pwd">The password</param>
        /// <returns>Hash password</returns>
        public static string GenerateBcryptHash(string pwd)
        {
            var passwordCharArray = pwd.ToCharArray();
            var salt = new byte[16];
            new SecureRandom().NextBytes(salt);
            //new Random().NextBytes(salt);
            return OpenBsdBCrypt.Generate(passwordCharArray, salt, 12);
        }

        /// <summary>
        /// Encrypt a byte array using AES 128
        /// </summary>
        /// <param name="key">128 bit key</param>
        /// <param name="secret">byte array that need to be encrypted</param>
        /// <returns>Encrypted array</returns>
        public static string EncryptAES(string key, byte[] secret)
        {
            using MemoryStream ms = new MemoryStream();
            byte[] keyBytes = GenerateKeyBytes(key);
            using AesManaged cryptor = new AesManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128
            };

            //We use the random generated iv created by AesManaged
            byte[] iv = cryptor.IV;

            using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateEncryptor(keyBytes, iv), CryptoStreamMode.Write))
            {
                cs.Write(secret, 0, secret.Length);
            }
            byte[] encryptedContent = ms.ToArray();

            //Create new byte array that should contain both unencrypted iv and encrypted data
            byte[] result = new byte[iv.Length + encryptedContent.Length];

            //copy our 2 array into one
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypt a byte array using AES 128
        /// </summary>
        /// <param name="key">key in bytes</param>
        /// <param name="secret">the encrypted bytes</param>
        /// <returns>decrypted bytes</returns>
        public static byte[] DecryptAES(string key, string secret)
        {
            byte[] iv = new byte[16]; //initial vector is 16 bytes
            byte[] secretBytes = Convert.FromBase64String(secret);
            byte[] encryptedContent = new byte[secretBytes.Length - 16]; //the rest should be encryptedcontent

            //Copy data to byte array
            Buffer.BlockCopy(secretBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(secretBytes, iv.Length, encryptedContent, 0, encryptedContent.Length);

            using MemoryStream ms = new MemoryStream();
            byte[] keyBytes = GenerateKeyBytes(key);
            using AesManaged cryptor = new AesManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128
            };

            using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateDecryptor(keyBytes, iv), CryptoStreamMode.Write))
            {
                cs.Write(encryptedContent, 0, encryptedContent.Length);
            }
            return ms.ToArray();
        }
        private static byte[] GenerateKeyBytes(string key)
        {
            byte[] originKeyBytes = Encoding.UTF8.GetBytes(key);
            byte[] keyBytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                keyBytes[i] = originKeyBytes[i % originKeyBytes.Length];
            }
            return keyBytes;
        }
    }
}
