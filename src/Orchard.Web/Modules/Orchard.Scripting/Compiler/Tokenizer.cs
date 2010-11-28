﻿using System;
using System.Text;

namespace Orchard.Scripting.Compiler {
    public class Tokenizer {
        private readonly string _expression;
        private readonly StringBuilder _stringBuilder;
        private int _index;
        private int _startTokenIndex;

        public Tokenizer(string expression) {
            _expression = expression;
            _stringBuilder = new StringBuilder();
        }

        public Token NextToken() {
            while (true) {
                if (Eof())
                    return CreateToken(TokenKind.Eof);

                _startTokenIndex = _index;
                char ch = Character();
                switch (ch) {
                    case '(':
                        NextCharacter();
                        return CreateToken(TokenKind.OpenParen);
                    case ')':
                        NextCharacter();
                        return CreateToken(TokenKind.CloseParen);
                    case ',':
                        NextCharacter();
                        return CreateToken(TokenKind.Comma);
                    case '+':
                        NextCharacter();
                        return CreateToken(TokenKind.Plus);
                    case '-':
                        NextCharacter();
                        return CreateToken(TokenKind.Minus);
                    case '*':
                        NextCharacter();
                        return CreateToken(TokenKind.Mul);
                    case '/':
                        NextCharacter();
                        return CreateToken(TokenKind.Div);
                    case '"':
                        return LexStringLiteral();
                    case '\'':
                        return LexSingleQuotedStringLiteral();
                }

                if (IsDigitCharacter(ch)) {
                    return LexInteger();
                }
                else if (IsIdentifierCharacter(ch)) {
                    return LexIdentifierOrKeyword();
                }
                else if (IsWhitespaceCharacter(ch)) {
                    NextCharacter();
                    continue;
                }

                return CreateToken(TokenKind.Invalid, "Unrecognized character");
            }
        }

        private Token LexIdentifierOrKeyword() {
            _stringBuilder.Clear();

            _stringBuilder.Append(Character());
            while (true) {
                NextCharacter();

                if (!Eof() && (IsIdentifierCharacter(Character()) || IsDigitCharacter(Character()))) {
                    _stringBuilder.Append(Character());
                }
                else {
                    return CreateIdentiferOrKeyword(_stringBuilder.ToString());
                }
            }
        }

        private Token LexInteger() {
            _stringBuilder.Clear();

            _stringBuilder.Append(Character());
            while (true) {
                NextCharacter();

                if (!Eof() && IsDigitCharacter(Character())) {
                    _stringBuilder.Append(Character());
                }
                else {
                    return CreateToken(TokenKind.Integer, Int32.Parse(_stringBuilder.ToString()));
                }
            }
        }

        private Token CreateIdentiferOrKeyword(string identifier) {
            switch (identifier) {
                case "true":
                    return CreateToken(TokenKind.True, true);
                case "false":
                    return CreateToken(TokenKind.False, false);
                case "or":
                    return CreateToken(TokenKind.Or, null);
                case "and":
                    return CreateToken(TokenKind.And, null);
                case "not":
                    return CreateToken(TokenKind.Not, null);
                default:
                    return CreateToken(TokenKind.Identifier, identifier);
            }
        }

        private bool IsWhitespaceCharacter(char character) {
            return char.IsWhiteSpace(character);
        }

        private bool IsIdentifierCharacter(char ch) {
            return
                (ch >= 'a' && ch <= 'z') ||
                (ch >= 'A' && ch <= 'Z') ||
                (ch == '_');
        }

        private bool IsDigitCharacter(char ch) {
            return ch >= '0' && ch <= '9';
        }

        private Token LexSingleQuotedStringLiteral() {
            _stringBuilder.Clear();

            while (true) {
                NextCharacter();

                if (Eof())
                    return CreateToken(TokenKind.Invalid, "Unterminated string literal");

                // Termination
                if (Character() == '\'') {
                    NextCharacter();
                    return CreateToken(TokenKind.SingleQuotedStringLiteral, _stringBuilder.ToString());
                }
                // backslash notation
                else if (Character() == '\\') {
                    NextCharacter();

                    if (Eof())
                        return CreateToken(TokenKind.Invalid, "Unterminated string literal");

                    if (Character() == '\\') {
                        _stringBuilder.Append('\\');
                    }
                    else if (Character() == '\'') {
                        _stringBuilder.Append('\'');
                    }
                    else {
                        _stringBuilder.Append('\\');
                        _stringBuilder.Append(Character());
                    }
                }
                // Regular character in string
                else {
                    _stringBuilder.Append(Character());
                }
            }
        }

        private Token LexStringLiteral() {
            _stringBuilder.Clear();

            while (true) {
                NextCharacter();

                if (Eof())
                    return CreateToken(TokenKind.Invalid, "Unterminated string literal");

                // Termination
                if (Character() == '"') {
                    NextCharacter();
                    return CreateToken(TokenKind.StringLiteral, _stringBuilder.ToString());
                }
                // backslash notation
                else if (Character() == '\\') {
                    NextCharacter();

                    if (Eof())
                        return CreateToken(TokenKind.Invalid, "Unterminated string literal");

                    _stringBuilder.Append(Character());
                }
                // Regular character in string
                else {
                    _stringBuilder.Append(Character());
                }
            }
        }

        private void NextCharacter() {
            _index++;
        }

        private char Character() {
            return _expression[_index];
        }

        private Token CreateToken(TokenKind kind, object value = null) {
            return new Token {
                Kind = kind,
                Position = _startTokenIndex,
                Value = value
            };
        }

        private bool Eof() {
            return (_index >= _expression.Length);
        }
    }
}