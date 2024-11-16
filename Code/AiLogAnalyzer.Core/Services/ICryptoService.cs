﻿namespace AiLogAnalyzer.Core.Services;

public interface ICryptoService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
}