/**
 * @contract M-WORKER-ID
 * @purpose Orchestrates identity saga with Wolverine: ProvisionUserSaga, SyncRoleSaga with DLQ integration
 * @module-type CORE_LOGIC
 * @depends M-SHARED-ID, M-CACHE-ID, M-KEYCLOAK-ID
 * @verification-ref V-M-WORKER-ID
 * @semantic-domain Saga, Identity Provisioning, Synchronization
 * @invariant ProvisionUserSaga: Create in Keycloak → Populate cache (TTL 60-300s)
 * @invariant SyncRoleSaga: Bidirectional sync Keycloak ↔ cache
 * @invariant Failed operations published to DLQ with original request
 * @error-strategy TimeoutException, KeycloakException, SyncException
 * @stability EVOLVING (Phase-5 in progress)
 */

namespace IdentityService.WorkerService
{
    /**
     * @domain-concept ProvisionUserSaga
     * @aggregate-root YES
     * @purpose Orchestrates user provisioning: Keycloak create → cache population
     * @invariant State: Pending → CreatingInKeycloak → PopulatingCache → Completed/Failed
     * @invariant Cache populated with TTL on saga completion
     */
    public class ProvisionUserSaga : Saga<ProvisionUserSagaState>
    {
        /**
         * @contract-action CreateUserInKeycloak
         * @param command CreateUserInKeycloakCommand
         * @log-event saga.identity-create-keycloak-start {email}
         * @log-event saga.identity-create-keycloak-success {userId}
         * @log-event saga.identity-create-keycloak-error {email} {error}
         * @trace-span saga.create-user-keycloak
         * @pre-condition saga.State == "Pending"
         * @post-condition saga.State == "CreatedInKeycloak" || saga.State == "CreationFailed"
         * @idempotent YES (ledger-based)
         */
        public void HandleCreateUserInKeycloak(CreateUserInKeycloakCommand command);

        /**
         * @contract-action PopulateCache
         * @param command PopulateCacheCommand with user + TTL
         * @log-event saga.identity-populate-cache-start {userId} ttl={ttlSeconds}s
         * @log-event saga.identity-populate-cache-success {userId}
         * @trace-span saga.populate-cache
         * @pre-condition saga.State == "CreatedInKeycloak"
         * @post-condition saga.State == "Completed"
         * @idempotent YES
         */
        public void HandlePopulateCache(PopulateCacheCommand command);

        /**
         * @contract-action CompensateOnFailure
         * @param failureContext Failure information
         * @log-event saga.identity-compensate-start {correlationId} {failedStep}
         * @log-event saga.identity-compensate-delete-keycloak {userId}
         * @log-event saga.identity-compensate-success {correlationId}
         * @trace-span saga.compensate-provision
         * @post-condition saga.State == "Compensated"
         */
        public void Compensate(FailureContext failureContext);

        /**
         * @contract-action PublishToDLQ
         * @param failureEvent FailedIdentityEvent with original request
         * @log-event saga.identity-dlq-publish {correlationId}
         * @trace-span saga.publish-dlq-identity
         * @idempotent NO
         */
        public Task PublishToDLQ(FailedIdentityEvent failureEvent, CancellationToken ct);
    };

    /**
     * @domain-concept SyncRoleSaga
     * @aggregate-root YES
     * @purpose Bidirectional role sync: Keycloak → cache or cache → Keycloak
     * @invariant Direction: inbound or outbound
     */
    public class SyncRoleSaga : Saga<SyncRoleSagaState>
    {
        /**
         * @contract-action SyncFromKeycloak
         * @param command SyncRoleFromKeycloakCommand
         * @log-event saga.role-sync-keycloak-start {userId} {roleName}
         * @log-event saga.role-sync-keycloak-success {userId} {roleName}
         * @trace-span saga.sync-role-keycloak
         * @idempotent YES
         */
        public void HandleSyncFromKeycloak(SyncRoleFromKeycloakCommand command);

        /**
         * @contract-action SyncToKeycloak
         * @param command SyncRoleToKeycloakCommand
         * @log-event saga.role-sync-cache-start {userId} {roleName}
         * @log-event saga.role-sync-cache-success {userId} {roleName}
         * @trace-span saga.sync-role-cache
         * @idempotent YES
         */
        public void HandleSyncToKeycloak(SyncRoleToKeycloakCommand command);
    };

    /**
     * @domain-concept ProvisionUserHandler
     * @purpose Step handler for CreateUserInKeycloak
     */
    public class ProvisionUserHandler
    {
        /**
         * @contract-action Handle
         * @param command CreateUserInKeycloakCommand
         * @param ct Cancellation token
         * @log-event worker.handler.provision-user-keycloak-call {email}
         * @log-event worker.handler.provision-user-keycloak-success {userId}
         * @trace-span worker.provision-user
         * @throws KeycloakException — creation failed
         * @idempotent YES
         */
        public Task Handle(CreateUserInKeycloakCommand command, CancellationToken ct);
    };

    /**
     * @domain-concept CompensationService
     * @purpose Compensation logic: delete user, revert roles
     */
    public interface ICompensationService
    {
        /**
         * @contract-action DeleteUserFromKeycloakAsync
         * @param userId User identifier
         * @param ct Cancellation token
         * @log-event worker.compensation.delete-keycloak {userId}
         * @trace-span worker.compensation.delete-user
         * @throws KeycloakException — deletion failed
         * @idempotent NO
         */
        Task DeleteUserFromKeycloakAsync(string userId, CancellationToken ct);

        /**
         * @contract-action DeleteUserFromCacheAsync
         * @param userId User identifier
         * @param ct Cancellation token
         * @log-event worker.compensation.delete-cache {userId}
         * @trace-span worker.compensation.delete-cache
         * @idempotent NO
         */
        Task DeleteUserFromCacheAsync(string userId, CancellationToken ct);
    };

    /**
     * @domain-concept IDLQPublisher
     * @purpose Publishes failed identity operations
     */
    public interface IDLQPublisher
    {
        /**
         * @contract-action PublishFailedIdentityAsync
         * @param failureEvent FailedIdentityEvent with original request + error
         * @param ct Cancellation token
         * @log-event worker.dlq.publish-failed-identity {correlationId}
         * @trace-span worker.dlq.publish-identity
         * @idempotent NO
         */
        Task PublishFailedIdentityAsync(FailedIdentityEvent failureEvent, CancellationToken ct);
    };
}
