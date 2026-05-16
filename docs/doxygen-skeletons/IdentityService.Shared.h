/**
 * @contract M-SHARED-ID
 * @purpose Provides shared DTOs, events, commands, validators for identity operations
 * @module-type UTILITY
 * @depends none (leaf module)
 * @verification-ref V-M-SHARED-ID
 * @semantic-domain Identity, User, Role
 * @invariant User email is unique
 * @invariant Role names follow domain conventions
 * @invariant FailedIdentityEvent preserves original request for DLQ processing
 * @error-strategy ValidationException, SerializationException
 * @stability STABLE
 */

namespace IdentityService.Shared
{
    /**
     * @domain-concept UserCreatedDto
     * @value-object YES
     * @invariant Email is valid RFC 5322 format
     * @invariant FirstName and LastName non-empty
     */
    public record UserCreatedDto
    {
        public string UserId { get; init; }
        public string Email { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
    };

    /**
     * @domain-concept RoleAssignDto
     * @value-object YES
     * @invariant RoleName valid for organization
     */
    public record RoleAssignDto
    {
        public string UserId { get; init; }
        public string RoleName { get; init; }
    };

    /**
     * @domain-concept FailedIdentityEvent
     * @value-object YES
     * @purpose DLQ pattern: preserves original request + error for manual intervention
     * @invariant OriginalRequest must be non-null for replay capability
     * @invariant ErrorReason must be descriptive
     */
    public record FailedIdentityEvent
    {
        public string CorrelationId { get; init; }
        public object OriginalRequest { get; init; }
        public string ErrorReason { get; init; }
        public DateTime FailedAt { get; init; }
    };

    /**
     * @domain-concept UserProvisioner (DDD Service)
     * @contract-action ProvisionUser
     * @param request UserCreatedDto with user details
     * @return ProvisioningResult with userId
     * @throws ValidationException — email invalid
     * @throws DuplicateKeyException — user already exists
     * @log-event shared.provisioner.provision-user-start {email}
     * @log-event shared.provisioner.provision-user-success {userId}
     * @trace-span shared.provision-user
     * @pre-condition request != null && request.Email != null
     * @post-condition result.UserId != null
     * @idempotent YES
     * @pure NO (I/O)
     */
    public class UserProvisioner
    {
    };

    /**
     * @domain-concept UserCreatedValidator
     * @contract-action Validate
     * @param request UserCreatedDto
     * @return ValidationResult
     * @log-event shared.validator.user-validate
     * @idempotent YES
     * @pure YES
     * @invariant Email format valid
     * @invariant Names non-empty
     */
    public class UserCreatedValidator : AbstractValidator<UserCreatedDto>
    {
    };
}
