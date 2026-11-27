// The error a Health query throws when the API answers 401: the reader is
// signed out, so the page asks for a sign-in instead of showing a failure.
export class SignInRequiredError extends Error {
  constructor() {
    super("Sign in to read check-ins.");
    this.name = "SignInRequiredError";
  }
}
