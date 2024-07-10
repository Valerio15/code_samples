using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using System.Text;

public class CustomTypewriterEffect : UnityUITypewriterEffect
{
    [Tooltip("Frequency of audio plays. 1 = every character, 2 = every other character, etc.")]
    public int frequency = 1;  // Default to playing sound every character.
    private int currentCharacterIndex = 0;
    private char currentCharacter;

    public override IEnumerator Play(int fromIndex = 0)
    {
        if ((control != null) && (charactersPerSecond > 0))
        {
            // Setup:
            InitAutoScroll();
            if (waitOneFrameBeforeStarting) yield return null;
            if (audioSource != null) audioSource.clip = audioClip;
            onBegin.Invoke();
            paused = false;
            float delay = 1 / charactersPerSecond;
            float lastTime = DialogueTime.time;
            float elapsed = 0;
            int charactersTyped = 0;
            if (original == null) original = control.text;
            tokens = Tokenize(original);
            openTokenTypes = new List<TokenType>();
            current = new StringBuilder();
            frontSkippedText = string.Empty;
            var preTyped = 0;
            if (fromIndex > 0)
            {
                // Add characters to skip ahead:
                int frontSafeguard = 0;
                while (preTyped < fromIndex && tokens.Count > 0 && frontSafeguard < 65535)
                {
                    frontSafeguard++;
                    var frontToken = GetNextToken(tokens);
                    switch (frontToken.tokenType)
                    {
                        case TokenType.Character:
                            preTyped++;
                            if (rightToLeft)
                            {
                                current.Insert(0, frontToken.character);
                            }
                            else
                            {
                                current.Append(frontToken.character);
                            }
                            break;
                        case TokenType.BoldOpen:
                        case TokenType.ItalicOpen:
                        case TokenType.ColorOpen:
                        case TokenType.SizeOpen:
                            OpenRichText(current, frontToken, openTokenTypes);
                            break;
                        case TokenType.BoldClose:
                        case TokenType.ItalicClose:
                        case TokenType.ColorClose:
                        case TokenType.SizeClose:
                            CloseRichText(current, frontToken, openTokenTypes);
                            break;
                        case TokenType.Quad:
                            current.Append(frontToken.code);
                            break;
                    }
                }
                control.text = GetCurrentText(current, openTokenTypes, tokens);
                charactersTyped = preTyped;
            }
            int safeguard = 0;
            while (tokens.Count > 0 && safeguard < 65535)
            {
                safeguard++;
                if (!paused)
                {
                    var deltaTime = DialogueTime.time - lastTime;

                    elapsed += deltaTime;
                    var goal = preTyped + (elapsed * charactersPerSecond);
                    var isCodeNext = false;
                    while (((charactersTyped < goal) || isCodeNext) && (tokens.Count > 0))
                    {
                        var token = GetNextToken(tokens);
                        switch (token.tokenType)
                        {
                            case TokenType.Character:
                                if (rightToLeft)
                                {
                                    current.Insert(0, token.character);
                                }
                                else
                                {
                                    current.Append(token.character);
                                }
                                if (IsSilentCharacter(token.character))
                                {
                                    if (stopAudioOnSilentCharacters) StopCharacterAudio();
                                }
                                else
                                {
                                    if (currentCharacterIndex == frequency - 1)
                                    {
                                        CustomPlayCharacterAudio(token.character);

                                        currentCharacterIndex = 0;

                                    }
                                    else
                                    {
                                        currentCharacterIndex += 1;

                                    }

                                }
                                onCharacter.Invoke();
                                ManageAudioPitch.Instance.SetCurrentCharacter(token.character);
                                charactersTyped++;
                                if (IsFullPauseCharacter(token.character))
                                {
                                    isCodeNext = (tokens.Count > 0) && (tokens[0].tokenType != TokenType.Character);
                                    control.text = frontSkippedText + GetCurrentText(current, openTokenTypes, tokens);
                                    yield return PauseForDuration(fullPauseDuration);
                                }
                                else if (IsQuarterPauseCharacter(token.character))
                                {
                                    isCodeNext = (tokens.Count > 0) && (tokens[0].tokenType != TokenType.Character);
                                    control.text = frontSkippedText + GetCurrentText(current, openTokenTypes, tokens);
                                    yield return PauseForDuration(quarterPauseDuration);
                                }
                                break;
                            case TokenType.BoldOpen:
                            case TokenType.ItalicOpen:
                            case TokenType.ColorOpen:
                            case TokenType.SizeOpen:
                                OpenRichText(current, token, openTokenTypes);
                                break;
                            case TokenType.BoldClose:
                            case TokenType.ItalicClose:
                            case TokenType.ColorClose:
                            case TokenType.SizeClose:
                                CloseRichText(current, token, openTokenTypes);
                                break;
                            case TokenType.Quad:
                                current.Append(token.code);
                                break;
                            case TokenType.Pause:
                                control.text = GetCurrentText(current, openTokenTypes, tokens);
                                yield return PauseForDuration(token.duration);
                                break;
                            case TokenType.InstantOpen:
                                AddInstantText(current, openTokenTypes, tokens);
                                break;
                        }
                        isCodeNext = (tokens.Count > 0) && (tokens[0].tokenType != TokenType.Character);
                    }
                }
                // Set the text:
                control.text = GetCurrentText(current, openTokenTypes, tokens);

                // Handle auto-scrolling:
                HandleAutoScroll(false);

                //---Uncomment the line below to debug: 
                //Debug.Log(control.text.Replace("<", "[").Replace(">", "]") + " " + name + " " + Time.frameCount, this);

                lastTime = DialogueTime.time;
                var delayTime = DialogueTime.time + delay;
                int delaySafeguard = 0;
                while (DialogueTime.time < delayTime && delaySafeguard < 999)
                {
                    delaySafeguard++;
                    yield return null;
                }
            }
        }
        Stop();
    }

    protected virtual void CustomPlayCharacterAudio(char character)
    {
        if (audioClip == null || audioSource == null) return;
        AudioClip randomClip = null;
        if (alternateAudioClips != null && alternateAudioClips.Length > 0)
        {
            if (ManageAudioPitch.Instance.makePredictable)
            {
                int hashCode = character.GetHashCode();
                int predictableIndex = hashCode % alternateAudioClips.Length;
                randomClip = alternateAudioClips[predictableIndex];
                Debug.Log("Played Audio Clip: " + alternateAudioClips[predictableIndex].name);
            }
            else
            {
                var randomIndex = UnityEngine.Random.Range(0, alternateAudioClips.Length + 1);
                randomClip = (randomIndex < alternateAudioClips.Length) ? alternateAudioClips[randomIndex] : audioClip;
            }

        }

        if (interruptAudioClip)
        {
            if (usePlayOneShot)
            {
                if (randomClip != null) audioSource.clip = randomClip;
                audioSource.PlayOneShot(audioSource.clip);
            }
            else
            {
                if (audioSource.isPlaying) audioSource.Stop();
                if (randomClip != null) audioSource.clip = randomClip;
                audioSource.Play();
            }
        }
        else
        {
            if (!audioSource.isPlaying)
            {
                if (randomClip != null) audioSource.clip = randomClip;
                if (usePlayOneShot)
                {
                    audioSource.PlayOneShot(audioSource.clip);
                }
                else
                {
                    audioSource.Play();
                }

            }
        }
    }
}