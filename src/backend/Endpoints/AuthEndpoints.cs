using System.Security.Claims;
using FluentValidation;
using Project.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;

namespace Project.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/public/auth/register", async (
        RegisterRequestDto request,
        IValidator<RegisterRequestDto> validator,
        IAuthService authService) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        ));
            }

            try
            {
                var passenger = await authService.RegisterPassengerAsync(request);

                var response = new
                {
                    passenger.User.Id,
                    passenger.Name,
                    passenger.HomeAddress,
                    passenger.PreferredPaymentMethod
                };

                return Results.Created($"/api/public/passengers/{passenger.User.Id}", response);
            }
            catch (InvalidOperationException ex) // e.g. duplicate email
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return Results.UnprocessableEntity(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem("Registration failed.");
            }


        });

        app.MapPost("/api/public/auth/login", async (
            LoginRequestDto request,
            IValidator<LoginRequestDto> validator,
            IAuthService authService,
            HttpContext httpContext) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            LoginResponseDto? loginResult = await authService.LoginAsync(request);
            if (loginResult is null)
            {
                return Results.Unauthorized();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, loginResult.Id.ToString()),
                new(ClaimTypes.Email, loginResult.Email),
                new(ClaimTypes.Role, loginResult.Role.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);


            return Results.Ok(loginResult);
        });

        app.MapPost("/api/public/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });


        app.MapGet("/api/public/auth/me", async
        (
            ClaimsPrincipal token,
            IAuthService service) =>
        {
            try
            {
                MeResponseDto? response = await service.GetMeAsync(token);
                return response is null ? Results.Unauthorized() : Results.Ok(response);
            }
            catch (Exception)
            {
                return Results.Problem($"Error during getting data occurred");
            }
        }).RequireAuthorization();

        app.MapPatch("/api/public/auth/me", async (
            ClaimsPrincipal token,
            UpdateMeRequestDto request,
            IValidator<UpdateMeRequestDto> validator,
            IAuthService service) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(
                    validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        ));
            }

            try
            {
                var response = await service.UpdateMeAsync(token, request);
                return response is null ? Results.Unauthorized() : Results.Ok(response);
            }
            catch (Exception)
            {
                return Results.Problem("Profile update failed.");
            }
        }).RequireAuthorization("Passenger");

        return app;
    }


}

